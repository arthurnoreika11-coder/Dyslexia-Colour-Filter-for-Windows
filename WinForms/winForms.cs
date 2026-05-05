using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinForms
{
    /// <summary>
    /// Main form that creates an overlay window for the dyslexia color filter.
    /// This form stays on top of all windows and applies a semi-transparent tint to improve readability for dyslexic users.
    /// </summary>
    public class ColourFilterForm : Form
    {
        // Window style constants for creating a transparent, layered overlay window
        private const int GwlExStyle = -20;         // Index for extended window style
        private const int WsExTransparent = 0x20;   // Makes window transparent to mouse/keyboard input
        private const int WsExToolWindow = 0x80;    // Creates a tool window (doesn't appear in taskbar)
        private const int WsExLayered = 0x80000;    // Enables layered window (required for transparency)

        private NotifyIcon trayIcon;                        // System tray icon for accessing the app
        private MenuItem enabledMenuItem;                   // Menu item to toggle filter on/off
        private SettingsForm settingsForm;                  // Reference to the settings dialog
        private Color selectedColour;                       // Current overlay tint color
        private string settingsFilePath;                    // Path to settings file
        private bool startEnabled;                          // Whether filter should start enabled

        // Import Windows API to get and set window styles for transparency behavior
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        /// <summary>
        /// Constructor initializes the color filter form with default settings and creates the system tray icon.
        /// </summary>
        public ColourFilterForm()
        {
            selectedColour = Color.FromArgb(255, 242, 168); // Default cream color
            settingsFilePath = Path.Combine(Application.StartupPath, "filter-settings.txt");
            startEnabled = true;

            // Configure the overlay window to be borderless, maximized, and always on top
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            ShowInTaskbar = false;

            LoadSettings();
            CreateTrayIcon();
            ApplySettings();
        }

        /// <summary>
        /// Called when the form is shown. Applies Windows API styles to make the overlay transparent to input
        /// and prevents it from appearing in the taskbar.
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Combine window styles to make this a transparent, layered tool window
            int style = GetWindowLong(Handle, GwlExStyle);
            style = style | WsExLayered | WsExTransparent | WsExToolWindow;
            SetWindowLong(Handle, GwlExStyle, style);

            ApplySettings();
        }

        /// <summary>
        /// Creates the system tray icon with context menu for enabling/disabling the filter and accessing settings.
        /// </summary>
        private void CreateTrayIcon()
        {
            enabledMenuItem = new MenuItem("Enabled", ToggleEnabled);
            enabledMenuItem.Checked = startEnabled;

            ContextMenu trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Settings...", ShowSettings);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add(enabledMenuItem);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Exit", ExitApp);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = SystemIcons.Application;
            trayIcon.Text = "Dyslexia Colour Filter";
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
        }

        /// <summary>
        /// Toggles the filter enabled/disabled state when the user clicks the menu item.
        /// </summary>
        private void ToggleEnabled(object sender, EventArgs e)
        {
            SetEnabled(!enabledMenuItem.Checked);
        }

        /// <summary>
        /// Shows the settings dialog window. Creates a new instance if one doesn't exist or has been disposed.
        /// </summary>
        private void ShowSettings(object sender, EventArgs e)
        {
            if (settingsForm == null || settingsForm.IsDisposed)
            {
                settingsForm = new SettingsForm(this);
            }

            settingsForm.Show();
            settingsForm.BringToFront();
        }

        /// <summary>
        /// Property to get the current overlay color.
        /// </summary>
        public Color FilterColour
        {
            get
            {
                return selectedColour;
            }
        }

        /// <summary>
        /// Property to get the current opacity as a percentage (0-100).
        /// </summary>
        public int OpacityPercent
        {
            get
            {
                return Convert.ToInt32(Opacity * 100);
            }
        }

        /// <summary>
        /// Property to check if the filter is currently enabled.
        /// </summary>
        public bool FilterEnabled
        {
            get
            {
                return enabledMenuItem != null && enabledMenuItem.Checked;
            }
        }

        /// <summary>
        /// Sets the overlay color and updates the display and settings file.
        /// </summary>
        public void SetFilterColour(Color colour)
        {
            selectedColour = colour;
            ApplySettings();
            SaveSettings();
        }

        /// <summary>
        /// Sets the overlay opacity as a percentage (clamped between 10-100) and saves to settings.
        /// </summary>
        public void SetOpacityPercent(int percent)
        {
            if (percent < 10)
            {
                percent = 10;
            }
            else if (percent > 100)
            {
                percent = 100;
            }

            Opacity = percent / 100.0;
            SaveSettings();
        }

        /// <summary>
        /// Enables or disables the filter overlay and updates the menu state and visibility.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            enabledMenuItem.Checked = enabled;
            Visible = enabled;
            SaveSettings();
        }

        /// <summary>
        /// Applies the current settings to the form by updating the background color and visibility.
        /// </summary>
        private void ApplySettings()
        {
            BackColor = selectedColour;

            if (enabledMenuItem != null)
            {
                Visible = enabledMenuItem.Checked;
            }
        }

        /// <summary>
        /// Loads settings from the settings file. Reads color, opacity, and enabled state.
        /// Applies defaults if the file doesn't exist or contains invalid values.
        /// </summary>
        private void LoadSettings()
        {
            Opacity = 0.5; // Default opacity

            if (!File.Exists(settingsFilePath))
            {
                return;
            }

            string[] lines = File.ReadAllLines(settingsFilePath);

            for (int i = 0; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(new char[] { '=' }, 2);

                if (parts.Length != 2)
                {
                    continue;
                }

                string key = parts[0];
                string value = parts[1];

                // Parse color from hexadecimal format
                if (key == "Colour")
                {
                    try
                    {
                        selectedColour = ColorTranslator.FromHtml(value);
                    }
                    catch
                    {
                        selectedColour = Color.FromArgb(255, 242, 168); // Default if parsing fails
                    }
                }
                // Parse opacity as a decimal between 0.1 and 1.0
                else if (key == "Opacity")
                {
                    double opacity;
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out opacity))
                    {
                        if (opacity >= 0.1 && opacity <= 1.0)
                        {
                            Opacity = opacity;
                        }
                    }
                }
                // Parse enabled state from "True" or "False"
                else if (key == "Enabled")
                {
                    startEnabled = value == "True";
                }
            }
        }

        /// <summary>
        /// Saves the current settings (color, opacity, enabled state) to the settings file.
        /// </summary>
        private void SaveSettings()
        {
            string enabled = "True";

            if (enabledMenuItem != null && !enabledMenuItem.Checked)
            {
                enabled = "False";
            }

            string[] lines = new string[]
            {
                "Colour=" + ColorTranslator.ToHtml(selectedColour),
                "Opacity=" + Opacity.ToString(CultureInfo.InvariantCulture),
                "Enabled=" + enabled
            };

            File.WriteAllLines(settingsFilePath, lines);
        }

        /// <summary>
        /// Event handler to close the application when user clicks Exit in tray menu.
        /// </summary>
        private void ExitApp(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Cleans up resources when the form closes, including disposing the settings form and tray icon.
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (settingsForm != null && !settingsForm.IsDisposed)
            {
                settingsForm.CloseForAppExit();
            }

            trayIcon.Visible = false;
            trayIcon.Dispose();

            base.OnFormClosed(e);
        }
    }

    /// <summary>
    /// Settings dialog window where users can configure the color filter options.
    /// </summary>
    /// <summary>
    /// Settings dialog window where users can configure the color filter options.
    /// </summary>
    public class SettingsForm : Form
    {
        private ColourFilterForm filterForm;                 // Reference to the main filter form
        private CheckBox enabledCheckBox;                    // Checkbox to enable/disable the filter
        private Button colourButton;                         // Button to open custom color picker
        private Button closeButton;                          // Button to close the settings window
        private Panel colourPreview;                         // Panel showing the current tint color preview
        private TrackBar opacitySlider;                      // Slider for controlling filter opacity
        private Label opacityValueLabel;                     // Shows current opacity percentage
        private ColourDiskButton[] swatchButtons;            // Array of preset color swatch buttons
        private ToolTip toolTip;                             // Tooltips for UI elements
        private bool isLoading;                              // Flag to prevent events during initialization
        private bool allowClose;                             // Flag to allow closing via Close() method

        /// <summary>
        /// Constructor initializes the settings dialog with UI controls and loads current filter settings.
        /// </summary>
        public SettingsForm(ColourFilterForm filterForm)
        {
            this.filterForm = filterForm;

            // Configure the dialog window
            Text = "Dyslexia Colour Filter";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(460, 330);
            BackColor = Color.FromArgb(245, 245, 247);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            TopMost = true;

            CreateControls();
            LoadCurrentSettings();
        }

        /// <summary>
        /// Creates and adds all UI controls to the settings form, including labels, buttons, sliders, and color swatches.
        /// </summary>
        private void CreateControls()
        {
            toolTip = new ToolTip();

            // Title label
            Label titleLabel = CreateLabel("Dyslexia Colour Filter", 28, 22, 300, 30, 15F, FontStyle.Bold);
            Controls.Add(titleLabel);

            // Introductory description label
            Label introLabel = CreateLabel("Choose a comfortable tint and strength for reading.", 28, 54, 380, 22, 9F, FontStyle.Regular);
            introLabel.ForeColor = Color.FromArgb(110, 110, 115);
            Controls.Add(introLabel);

            // Enable/disable checkbox
            enabledCheckBox = new CheckBox();
            enabledCheckBox.Text = "Enabled";
            enabledCheckBox.Location = new Point(28, 92);
            enabledCheckBox.AutoSize = true;
            enabledCheckBox.FlatStyle = FlatStyle.System;
            enabledCheckBox.CheckedChanged += EnabledCheckBox_CheckedChanged;
            Controls.Add(enabledCheckBox);

            // Color selection section header
            Label colourLabel = CreateSectionLabel("Tint", 28, 132);
            Controls.Add(colourLabel);

            // Color preview panel showing the currently selected color
            colourPreview = new Panel();
            colourPreview.Location = new Point(380, 128);
            colourPreview.Size = new Size(42, 28);
            colourPreview.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(colourPreview);

            // Create preset color swatch buttons
            ColourDiskButton creamSwatch = CreateSwatchButton("Cream", Color.FromArgb(255, 242, 168), 28, 166);
            ColourDiskButton pinkSwatch = CreateSwatchButton("Rose", Color.FromArgb(255, 215, 229), 90, 166);
            ColourDiskButton blueSwatch = CreateSwatchButton("Sky", Color.FromArgb(180, 220, 255), 152, 166);
            ColourDiskButton mintSwatch = CreateSwatchButton("Mint", Color.FromArgb(200, 255, 200), 214, 166);
            ColourDiskButton greySwatch = CreateSwatchButton("Mist", Color.FromArgb(216, 221, 230), 276, 166);

            swatchButtons = new ColourDiskButton[]
            {
                creamSwatch,
                pinkSwatch,
                blueSwatch,
                mintSwatch,
                greySwatch
            };

            for (int i = 0; i < swatchButtons.Length; i++)
            {
                Controls.Add(swatchButtons[i]);
            }

            // Custom color picker button
            colourButton = new Button();
            colourButton.Text = "Custom";
            colourButton.Location = new Point(356, 171);
            colourButton.Size = new Size(72, 32);
            StyleSecondaryButton(colourButton);
            colourButton.Click += ColourButton_Click;
            toolTip.SetToolTip(colourButton, "Choose custom colour");
            Controls.Add(colourButton);

            // Opacity section header
            Label opacityLabel = CreateSectionLabel("Opacity", 28, 230);
            Controls.Add(opacityLabel);

            // Opacity slider for adjusting transparency
            opacitySlider = new TrackBar();
            opacitySlider.Location = new Point(28, 256);
            opacitySlider.Size = new Size(320, 45);
            opacitySlider.Minimum = 10;
            opacitySlider.Maximum = 100;
            opacitySlider.TickFrequency = 10;
            opacitySlider.ValueChanged += OpacitySlider_ValueChanged;
            Controls.Add(opacitySlider);

            // Label showing current opacity percentage
            opacityValueLabel = new Label();
            opacityValueLabel.Location = new Point(366, 262);
            opacityValueLabel.Size = new Size(56, 24);
            opacityValueLabel.TextAlign = ContentAlignment.MiddleRight;
            opacityValueLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            opacityValueLabel.ForeColor = Color.FromArgb(29, 29, 31);
            Controls.Add(opacityValueLabel);

            // Close button
            closeButton = new Button();
            closeButton.Text = "Close";
            closeButton.Location = new Point(338, 294);
            closeButton.Size = new Size(90, 30);
            StylePrimaryButton(closeButton);
            closeButton.Click += CloseButton_Click;
            Controls.Add(closeButton);
        }

        /// <summary>
        /// Loads the current filter settings and updates all UI controls to reflect them.
        /// </summary>
        private void LoadCurrentSettings()
        {
            isLoading = true;
            enabledCheckBox.Checked = filterForm.FilterEnabled;
            colourPreview.BackColor = filterForm.FilterColour;
            opacitySlider.Value = filterForm.OpacityPercent;
            UpdateOpacityLabel();
            UpdateSelectedSwatch();
            isLoading = false;
        }

        /// <summary>
        /// Event handler for the enabled checkbox. Updates the filter form when toggled.
        /// </summary>
        private void EnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (isLoading)
            {
                return;
            }

            filterForm.SetEnabled(enabledCheckBox.Checked);
        }

        /// <summary>
        /// Event handler for the custom color button. Opens the color picker dialog and applies selected color.
        /// </summary>
        private void ColourButton_Click(object sender, EventArgs e)
        {
            ColorDialog colourDialog = new ColorDialog();
            colourDialog.Color = filterForm.FilterColour;
            colourDialog.FullOpen = true;

            if (colourDialog.ShowDialog(this) == DialogResult.OK)
            {
                colourPreview.BackColor = colourDialog.Color;
                filterForm.SetFilterColour(colourDialog.Color);
                UpdateSelectedSwatch();
            }

            colourDialog.Dispose();
        }

        /// <summary>
        /// Event handler for the opacity slider. Updates the opacity label and applies the new value.
        /// </summary>
        private void OpacitySlider_ValueChanged(object sender, EventArgs e)
        {
            UpdateOpacityLabel();

            if (isLoading)
            {
                return;
            }

            filterForm.SetOpacityPercent(opacitySlider.Value);
        }

        /// <summary>
        /// Updates the opacity value label to show the current slider value as a percentage.
        /// </summary>
        private void UpdateOpacityLabel()
        {
            opacityValueLabel.Text = opacitySlider.Value + "%";
        }

        /// <summary>
        /// Helper method to create a formatted label with specified text, position, size, and font style.
        /// </summary>
        private Label CreateLabel(string text, int x, int y, int width, int height, float size, FontStyle style)
        {
            Label label = new Label();
            label.Text = text;
            label.Location = new Point(x, y);
            label.Size = new Size(width, height);
            label.Font = new Font("Segoe UI", size, style);
            label.ForeColor = Color.FromArgb(29, 29, 31);
            return label;
        }

        /// <summary>
        /// Helper method to create a section header label with specific formatting.
        /// </summary>
        private Label CreateSectionLabel(string text, int x, int y)
        {
            Label label = CreateLabel(text, x, y, 160, 22, 9.5F, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(72, 72, 74);
            return label;
        }

        /// <summary>
        /// Helper method to create a color swatch button with specified name, color, and position.
        /// </summary>
        private ColourDiskButton CreateSwatchButton(string name, Color colour, int x, int y)
        {
            ColourDiskButton button = new ColourDiskButton();
            button.AccessibleName = name;
            button.Tag = colour;       // Store the color as button tag
            button.DiskColour = colour;
            button.Location = new Point(x, y);
            button.Size = new Size(46, 46);
            button.Text = "";
            button.Click += SwatchButton_Click;
            toolTip.SetToolTip(button, name);
            return button;
        }

        /// <summary>
        /// Styles a button with primary appearance (blue background, white text).
        /// </summary>
        private void StylePrimaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.FromArgb(0, 122, 255);
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Styles a button with secondary appearance (white background, blue text, blue border).
        /// </summary>
        private void StyleSecondaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(0, 122, 255);
            button.FlatAppearance.BorderColor = Color.FromArgb(210, 210, 215);
            button.FlatAppearance.BorderSize = 1;
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Event handler for clicking a preset color swatch button. Applies the selected color to the filter.
        /// </summary>
        private void SwatchButton_Click(object sender, EventArgs e)
        {
            ColourDiskButton button = sender as ColourDiskButton;

            if (button == null)
            {
                return;
            }

            Color colour = (Color)button.Tag;
            colourPreview.BackColor = colour;
            filterForm.SetFilterColour(colour);
            UpdateSelectedSwatch();
        }

        /// <summary>
        /// Updates which color swatch button is visually marked as selected based on the current filter color.
        /// </summary>
        private void UpdateSelectedSwatch()
        {
            if (swatchButtons == null)
            {
                return;
            }

            Color selected = filterForm.FilterColour;

            for (int i = 0; i < swatchButtons.Length; i++)
            {
                Color swatchColour = (Color)swatchButtons[i].Tag;
                swatchButtons[i].IsSelected = swatchColour.ToArgb() == selected.ToArgb();
            }
        }

        /// <summary>
        /// Event handler for the close button. Hides the settings form without exiting the app.
        /// </summary>
        private void CloseButton_Click(object sender, EventArgs e)
        {
            Hide();
        }

        /// <summary>
        /// Allows the settings form to close when the main application is exiting.
        /// </summary>
        public void CloseForAppExit()
        {
            allowClose = true;
            Close();
        }

        /// <summary>
        /// Prevents the settings form from closing when the user clicks the X button,
        /// but allows it to close when called by CloseForAppExit().
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnFormClosing(e);
        }
    }

    /// <summary>
    /// Custom button control that displays a colored disk (circle) with selection and hover visual feedback.
    /// Used for the color swatch buttons in the settings dialog.
    /// </summary>
    /// <summary>
    /// Custom button control that displays a colored disk (circle) with selection and hover visual feedback.
    /// Used for the color swatch buttons in the settings dialog.
    /// </summary>
    public class ColourDiskButton : Button
    {
        private bool isHovered;     // Whether the mouse is hovering over the button
        private bool isSelected;    // Whether this swatch is currently selected
        private Color diskColour;   // The color displayed in the disk

        /// <summary>
        /// Constructor initializes the button with a white disk color and custom drawing mode.
        /// </summary>
        public ColourDiskButton()
        {
            diskColour = Color.White;
            // Enable custom painting for this button
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            TabStop = true;
        }

        /// <summary>
        /// Property to get or set the color displayed in the disk. Setting triggers a redraw.
        /// </summary>
        public Color DiskColour
        {
            get
            {
                return diskColour;
            }
            set
            {
                diskColour = value;
                Invalidate();  // Redraw the button
            }
        }

        /// <summary>
        /// Property to get or set whether this swatch is selected. Setting triggers a redraw.
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;
                Invalidate();  // Redraw the button
            }
        }

        /// <summary>
        /// Event handler when the mouse enters the button. Updates hover state and redraws.
        /// </summary>
        protected override void OnMouseEnter(EventArgs e)
        {
            isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        /// <summary>
        /// Event handler when the mouse leaves the button. Updates hover state and redraws.
        /// </summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        /// <summary>
        /// Custom paint method that draws the colored disk with selection and hover rings.
        /// </summary>
        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics graphics = pevent.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;  // Enable smooth anti-aliased drawing
            
            // Get background color from parent or use default
            Color backgroundColour = Parent == null ? Color.FromArgb(245, 245, 247) : Parent.BackColor;
            graphics.Clear(backgroundColour);

            // Rectangle for the outer ring (for selection/hover indicator)
            Rectangle outer = new Rectangle(2, 2, Width - 5, Height - 5);
            // Rectangle for the inner disk (the actual color circle)
            Rectangle inner = new Rectangle(7, 7, Width - 15, Height - 15);

            // Draw selection or hover ring if the button is selected, hovered, or focused
            if (isHovered || isSelected || Focused)
            {
                // Blue ring for selected state, gray ring for hover/focus
                Color ringColour = isSelected ? Color.FromArgb(0, 122, 255) : Color.FromArgb(174, 174, 178);
                // Thicker pen for selection (3px), thinner for hover (2px)
                using (Pen ringPen = new Pen(ringColour, isSelected ? 3F : 2F))
                {
                    graphics.DrawEllipse(ringPen, outer);
                }
            }

            // Draw the colored disk
            using (SolidBrush diskBrush = new SolidBrush(diskColour))
            {
                graphics.FillEllipse(diskBrush, inner);  // Fill with the disk color
            }

            // Draw a thin border around the disk
            using (Pen diskBorder = new Pen(Color.FromArgb(210, 210, 215), 1F))
            {
                graphics.DrawEllipse(diskBorder, inner);
            }
        }
    }

    /// <summary>
    /// Program entry point. Initializes and runs the main filter form.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main method. Starts the Windows Forms application with the color filter overlay.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            // Enable visual styles for modern Windows appearance
            Application.EnableVisualStyles();
            // Use compatible text rendering on all platforms
            Application.SetCompatibleTextRenderingDefault(false);
            // Start the application with the main filter form
            Application.Run(new ColourFilterForm());
        }
    }
}
