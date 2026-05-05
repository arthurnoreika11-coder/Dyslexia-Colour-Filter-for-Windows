using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
        /// Reapplies click-through window styles whenever the overlay becomes visible,
        /// since showing/hiding a form can reset extended window styles.
        /// </summary>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && Handle != IntPtr.Zero)
            {
                int style = GetWindowLong(Handle, GwlExStyle);
                style = style | WsExLayered | WsExTransparent | WsExToolWindow;
                SetWindowLong(Handle, GwlExStyle, style);
            }
        }

        /// <summary>
        /// Creates the system tray icon with context menu for enabling/disabling the filter and accessing settings.
        /// </summary>
        private void CreateTrayIcon()
        {
            enabledMenuItem = new MenuItem("Toggle Overlay", ToggleEnabled);
            enabledMenuItem.Checked = startEnabled;

            ContextMenu trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Settings...", ShowSettings);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Reset to Default", ResetToDefault);
            trayMenu.MenuItems.Add(enabledMenuItem);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Exit", ExitApp);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = CreateFilterIcon(selectedColour);
            string initialStatus = startEnabled ? "Enabled" : "Disabled";
            trayIcon.Text = string.Format("Dyslexia Colour Filter - {0}, {1}, {2}%", initialStatus, ColorTranslator.ToHtml(selectedColour), OpacityPercent);
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
        }

        /// <summary>
        /// Creates a custom tray icon representing the current filter color.
        /// </summary>
        private Icon CreateFilterIcon(Color color)
        {
            // Create a 16x16 bitmap for the icon
            Bitmap bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Draw a circle filled with the filter color
                using (SolidBrush brush = new SolidBrush(color))
                {
                    g.FillEllipse(brush, 2, 2, 12, 12);
                }

                // Draw a subtle border
                using (Pen pen = new Pen(Color.FromArgb(100, 0, 0, 0), 1))
                {
                    g.DrawEllipse(pen, 2, 2, 11, 11);
                }
            }

            // Convert bitmap to icon
            return Icon.FromHandle(bitmap.GetHicon());
        }

        /// <summary>
        /// Updates the tray icon to reflect the current filter color.
        /// </summary>
        private void UpdateTrayIcon()
        {
            if (trayIcon != null)
            {
                trayIcon.Icon = CreateFilterIcon(selectedColour);
            }
        }

        /// <summary>
        /// Updates the tray icon tooltip to show current settings.
        /// </summary>
        private void UpdateTrayText()
        {
            if (trayIcon != null)
            {
                string status = FilterEnabled ? "Enabled" : "Disabled";
                trayIcon.Text = string.Format("Dyslexia Colour Filter - {0}, {1}, {2}%", status, ColorTranslator.ToHtml(selectedColour), OpacityPercent);
            }
        }

        /// <summary>
        /// Toggles the filter enabled/disabled state when the user clicks the menu item.
        /// </summary>
        private void ToggleEnabled(object sender, EventArgs e)
        {
            SetEnabled(!enabledMenuItem.Checked);
        }

        /// <summary>
        /// Resets the filter to default settings (cream color, 50% opacity, enabled).
        /// </summary>
        private void ResetToDefault(object sender, EventArgs e)
        {
            SetFilterColour(Color.FromArgb(255, 242, 168)); // Default cream color
            SetOpacityPercent(50);
            SetEnabled(true);
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
            UpdateTrayIcon();
            UpdateTrayText();
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
            UpdateTrayText();
        }

        /// <summary>
        /// Enables or disables the filter overlay and updates the menu state and visibility.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            enabledMenuItem.Checked = enabled;
            Visible = enabled;
            SaveSettings();
            UpdateTrayText();
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
        private Button applyButton;                          // Button to apply the selected color
        private Button closeButton;                          // Button to close the settings window
        private Panel colourPreview;                         // Panel showing the current tint color preview
        private Panel textPreviewPanel;                      // Panel showing sample text with color overlay
        private Label textPreviewLabel;                      // Label with sample text inside preview panel
        private TrackBar opacitySlider;                      // Slider for controlling filter opacity
        private Label opacityValueLabel;                     // Shows current opacity percentage
        private ColourDiskButton[] swatchButtons;            // Array of preset color swatch buttons
        private ToolTip toolTip;                             // Tooltips for UI elements
        private Color previewColour;                         // Tracks the color being previewed before applying
        private bool isLoading;                              // Flag to prevent events during initialization
        private bool allowClose;                             // Flag to allow closing via Close() method

        /// <summary>
        /// Constructor initializes the settings dialog with UI controls and loads current filter settings.
        /// </summary>
        public SettingsForm(ColourFilterForm filterForm)
        {
            this.filterForm = filterForm;

            // Configure the dialog window with Windows 11 styling
            Text = "Dyslexia Colour Filter";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(460, 680);
            BackColor = Color.FromArgb(243, 243, 243);  // Windows 11 light background
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
            titleLabel.ForeColor = Color.FromArgb(31, 31, 31);  // Windows 11 dark text
            Controls.Add(titleLabel);

            // Introductory description label
            Label introLabel = CreateLabel("Choose a comfortable tint and strength for reading.", 28, 54, 380, 22, 9F, FontStyle.Regular);
            introLabel.ForeColor = Color.FromArgb(115, 115, 115);  // Windows 11 secondary text
            Controls.Add(introLabel);

            // Enable/disable checkbox
            enabledCheckBox = new CheckBox();
            enabledCheckBox.Text = "Enabled";
            enabledCheckBox.Location = new Point(28, 92);
            enabledCheckBox.AutoSize = true;
            enabledCheckBox.FlatStyle = FlatStyle.Flat;
            enabledCheckBox.ForeColor = Color.FromArgb(31, 31, 31);
            enabledCheckBox.BackColor = Color.FromArgb(243, 243, 243);
            enabledCheckBox.CheckedChanged += EnabledCheckBox_CheckedChanged;
            Controls.Add(enabledCheckBox);

            // Color selection section header
            Label colourLabel = CreateSectionLabel("Tint", 28, 132);
            Controls.Add(colourLabel);

            // Color preview panel showing the currently selected color
            colourPreview = new Panel();
            colourPreview.Location = new Point(380, 128);
            colourPreview.Size = new Size(42, 28);
            colourPreview.BorderStyle = BorderStyle.None;
            colourPreview.BackColor = filterForm.FilterColour;
            Controls.Add(colourPreview);

            // Create preset color swatch buttons
            ColourDiskButton creamSwatch = CreateSwatchButton("Cream", Color.FromArgb(255, 242, 168), 28, 166);
            ColourDiskButton pinkSwatch = CreateSwatchButton("Rose", Color.FromArgb(255, 215, 229), 90, 166);
            ColourDiskButton blueSwatch = CreateSwatchButton("Sky", Color.FromArgb(180, 220, 255), 152, 166);
            ColourDiskButton mintSwatch = CreateSwatchButton("Mint", Color.FromArgb(200, 255, 200), 214, 166);
            ColourDiskButton greySwatch = CreateSwatchButton("Mist", Color.FromArgb(216, 221, 230), 276, 166);

            // Group 1: Classic Off-White Paper
            Label clasicGroupLabel = CreateSectionLabel("Classic Off-White Paper", 28, 210);
            Controls.Add(clasicGroupLabel);

            ColourDiskButton classicParchmentSwatch = CreateSwatchButton("Classic Parchment", Color.FromArgb(241, 233, 210), 28, 238);
            ColourDiskButton linenSwatch = CreateSwatchButton("Linen", Color.FromArgb(250, 240, 230), 90, 238);
            ColourDiskButton oldLaceSwatch = CreateSwatchButton("Old Lace", Color.FromArgb(253, 245, 230), 152, 238);
            ColourDiskButton beigeSwitch = CreateSwatchButton("Beige", Color.FromArgb(245, 245, 220), 214, 238);

            // Group 2: Warm / Aged Paper
            Label agedGroupLabel = CreateSectionLabel("Warm / Aged Paper", 28, 292);
            Controls.Add(agedGroupLabel);

            ColourDiskButton warmPaperSwatch = CreateSwatchButton("Very Light Textured", Color.FromArgb(246, 238, 227), 28, 320);
            ColourDiskButton agedSandpaperSwatch = CreateSwatchButton("Aged Sandpaper", Color.FromArgb(229, 203, 186), 90, 320);
            ColourDiskButton warmYellowSwatch = CreateSwatchButton("Warm Yellow", Color.FromArgb(245, 239, 223), 152, 320);

            // Group 3: Light Textured Paper
            Label texturedGroupLabel = CreateSectionLabel("Light Textured Paper", 28, 374);
            Controls.Add(texturedGroupLabel);

            ColourDiskButton texturedBeigeSwatch = CreateSwatchButton("Textured Beige", Color.FromArgb(238, 231, 215), 28, 402);
            ColourDiskButton tanPaperSwatch = CreateSwatchButton("Tan Paper", Color.FromArgb(217, 189, 165), 90, 402);

            swatchButtons = new ColourDiskButton[]
            {
                creamSwatch,
                pinkSwatch,
                blueSwatch,
                mintSwatch,
                greySwatch,
                classicParchmentSwatch,
                linenSwatch,
                oldLaceSwatch,
                beigeSwitch,
                warmPaperSwatch,
                agedSandpaperSwatch,
                warmYellowSwatch,
                texturedBeigeSwatch,
                tanPaperSwatch
            };

            for (int i = 0; i < swatchButtons.Length; i++)
            {
                Controls.Add(swatchButtons[i]);
            }

            // Custom color picker button
            colourButton = new Button();
            colourButton.Text = "Custom";
            colourButton.Location = new Point(356, 173);
            colourButton.Size = new Size(72, 32);
            StyleSecondaryButton(colourButton);
            colourButton.Click += ColourButton_Click;
            toolTip.SetToolTip(colourButton, "Choose custom colour");
            Controls.Add(colourButton);

            // Opacity section header
            Label opacityLabel = CreateSectionLabel("Opacity", 28, 456);
            Controls.Add(opacityLabel);

            // Opacity slider for adjusting transparency
            opacitySlider = new TrackBar();
            opacitySlider.Location = new Point(28, 482);
            opacitySlider.Size = new Size(320, 45);
            opacitySlider.Minimum = 10;
            opacitySlider.Maximum = 100;
            opacitySlider.TickFrequency = 10;
            opacitySlider.ValueChanged += OpacitySlider_ValueChanged;
            Controls.Add(opacitySlider);

            // Label showing current opacity percentage
            opacityValueLabel = new Label();
            opacityValueLabel.Location = new Point(366, 488);
            opacityValueLabel.Size = new Size(56, 24);
            opacityValueLabel.TextAlign = ContentAlignment.MiddleRight;
            opacityValueLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            opacityValueLabel.ForeColor = Color.FromArgb(0, 120, 215);  // Windows 11 accent blue for emphasis
            opacityValueLabel.BackColor = Color.FromArgb(243, 243, 243);
            Controls.Add(opacityValueLabel);

            // Preview section header
            Label previewLabel = CreateSectionLabel("Preview", 28, 530);
            Controls.Add(previewLabel);

            // Text preview panel showing the color overlay effect
            textPreviewPanel = new Panel();
            textPreviewPanel.Location = new Point(28, 556);
            textPreviewPanel.Size = new Size(400, 50);
            textPreviewPanel.BorderStyle = BorderStyle.None;
            textPreviewPanel.BackColor = filterForm.FilterColour;
            Controls.Add(textPreviewPanel);

            // Sample text label inside the preview panel
            textPreviewLabel = new Label();
            textPreviewLabel.Text = "The quick brown fox jumps over the lazy dog";
            textPreviewLabel.Location = new Point(0, 0);
            textPreviewLabel.Size = new Size(400, 50);
            textPreviewLabel.TextAlign = ContentAlignment.MiddleLeft;
            textPreviewLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular);
            textPreviewLabel.ForeColor = Color.FromArgb(31, 31, 31);  // Dark text for contrast
            textPreviewLabel.BackColor = Color.Transparent;
            textPreviewLabel.Padding = new Padding(8, 0, 0, 0);
            textPreviewPanel.Controls.Add(textPreviewLabel);

            // Apply button
            applyButton = new Button();
            applyButton.Text = "Apply";
            applyButton.Location = new Point(238, 630);
            applyButton.Size = new Size(90, 34);
            StylePrimaryButton(applyButton);
            applyButton.Click += ApplyButton_Click;
            toolTip.SetToolTip(applyButton, "Apply colour to overlay");
            Controls.Add(applyButton);

            // Close button
            closeButton = new Button();
            closeButton.Text = "Close";
            closeButton.Location = new Point(338, 630);
            closeButton.Size = new Size(90, 34);
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
            previewColour = filterForm.FilterColour;
            colourPreview.BackColor = filterForm.FilterColour;
            textPreviewPanel.BackColor = previewColour;
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
            colourDialog.Color = previewColour;
            colourDialog.FullOpen = true;

            if (colourDialog.ShowDialog(this) == DialogResult.OK)
            {
                previewColour = colourDialog.Color;
                textPreviewPanel.BackColor = previewColour;
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
            label.ForeColor = Color.FromArgb(31, 31, 31);  // Windows 11 dark text
            label.BackColor = Color.FromArgb(243, 243, 243);  // Transparent against form background
            return label;
        }

        /// <summary>
        /// Helper method to create a section header label with specific formatting.
        /// </summary>
        private Label CreateSectionLabel(string text, int x, int y)
        {
            Label label = CreateLabel(text, x, y, 160, 22, 9.5F, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(31, 31, 31);  // Windows 11 dark text
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
        /// Styles a button with primary appearance (Windows 11 blue background, white text).
        /// </summary>
        private void StylePrimaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.FromArgb(0, 120, 215);  // Windows 11 accent blue
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 100, 200);  // Darker blue on hover
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 80, 160);   // Even darker on click
            button.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            button.Cursor = Cursors.Hand;
            button.Height = 34;  // Better vertical padding
        }

        /// <summary>
        /// Styles a button with secondary appearance (light background, blue text, subtle border).
        /// </summary>
        private void StyleSecondaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.FromArgb(243, 243, 243);  // Windows 11 light background
            button.ForeColor = Color.FromArgb(0, 120, 215);    // Windows 11 accent blue
            button.FlatAppearance.BorderColor = Color.FromArgb(204, 204, 204);  // Subtle border
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(237, 237, 237);  // Lighter on hover
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(225, 225, 225);  // More pressed
            button.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            button.Cursor = Cursors.Hand;
            button.Height = 32;  // Better vertical padding
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

            previewColour = (Color)button.Tag;
            textPreviewPanel.BackColor = previewColour;
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

            for (int i = 0; i < swatchButtons.Length; i++)
            {
                Color swatchColour = (Color)swatchButtons[i].Tag;
                swatchButtons[i].IsSelected = swatchColour.ToArgb() == previewColour.ToArgb();
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
        /// Event handler for the apply button. Applies the preview color to the main filter overlay.
        /// </summary>
        private void ApplyButton_Click(object sender, EventArgs e)
        {
            colourPreview.BackColor = previewColour;
            filterForm.SetFilterColour(previewColour);
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
            
            // Get background color from parent or use default (Windows 11 light)
            Color backgroundColour = Parent == null ? Color.FromArgb(243, 243, 243) : Parent.BackColor;
            graphics.Clear(backgroundColour);

            // Rectangle for the outer ring (for selection/hover indicator)
            Rectangle outer = new Rectangle(2, 2, Width - 5, Height - 5);
            // Rectangle for the inner disk (the actual color circle)
            Rectangle inner = new Rectangle(7, 7, Width - 15, Height - 15);

            // Draw selection or hover ring if the button is selected, hovered, or focused
            if (isHovered || isSelected || Focused)
            {
                // Windows 11 accent blue for selected state, gray ring for hover/focus
                Color ringColour = isSelected ? Color.FromArgb(0, 120, 215) : Color.FromArgb(163, 163, 163);
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
            using (Pen diskBorder = new Pen(Color.FromArgb(204, 204, 204), 1F))
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