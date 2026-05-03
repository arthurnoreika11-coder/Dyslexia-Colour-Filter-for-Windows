using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinForms
{
    public class ColourFilterForm : Form
    {
        private const int GwlExStyle = -20;
        private const int WsExTransparent = 0x20;
        private const int WsExToolWindow = 0x80;
        private const int WsExLayered = 0x80000;

        private NotifyIcon trayIcon;
        private MenuItem enabledMenuItem;
        private SettingsForm settingsForm;
        private Color selectedColour;
        private string settingsFilePath;
        private bool startEnabled;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public ColourFilterForm()
        {
            selectedColour = Color.FromArgb(255, 242, 168);
            settingsFilePath = Path.Combine(Application.StartupPath, "filter-settings.txt");
            startEnabled = true;

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            ShowInTaskbar = false;

            LoadSettings();
            CreateTrayIcon();
            ApplySettings();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            int style = GetWindowLong(Handle, GwlExStyle);
            style = style | WsExLayered | WsExTransparent | WsExToolWindow;
            SetWindowLong(Handle, GwlExStyle, style);

            ApplySettings();
        }

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

        private void ToggleEnabled(object sender, EventArgs e)
        {
            SetEnabled(!enabledMenuItem.Checked);
        }

        private void ShowSettings(object sender, EventArgs e)
        {
            if (settingsForm == null || settingsForm.IsDisposed)
            {
                settingsForm = new SettingsForm(this);
            }

            settingsForm.Show();
            settingsForm.BringToFront();
        }

        public Color FilterColour
        {
            get
            {
                return selectedColour;
            }
        }

        public int OpacityPercent
        {
            get
            {
                return Convert.ToInt32(Opacity * 100);
            }
        }

        public bool FilterEnabled
        {
            get
            {
                return enabledMenuItem != null && enabledMenuItem.Checked;
            }
        }

        public void SetFilterColour(Color colour)
        {
            selectedColour = colour;
            ApplySettings();
            SaveSettings();
        }

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

        public void SetEnabled(bool enabled)
        {
            enabledMenuItem.Checked = enabled;
            Visible = enabled;
            SaveSettings();
        }

        private void ApplySettings()
        {
            BackColor = selectedColour;

            if (enabledMenuItem != null)
            {
                Visible = enabledMenuItem.Checked;
            }
        }

        private void LoadSettings()
        {
            Opacity = 0.5;

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

                if (key == "Colour")
                {
                    try
                    {
                        selectedColour = ColorTranslator.FromHtml(value);
                    }
                    catch
                    {
                        selectedColour = Color.FromArgb(255, 242, 168);
                    }
                }
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
                else if (key == "Enabled")
                {
                    startEnabled = value == "True";
                }
            }
        }

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

        private void ExitApp(object sender, EventArgs e)
        {
            Close();
        }

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

    public class SettingsForm : Form
    {
        private ColourFilterForm filterForm;
        private CheckBox enabledCheckBox;
        private Button colourButton;
        private Button closeButton;
        private Panel colourPreview;
        private TrackBar opacitySlider;
        private Label opacityValueLabel;
        private ColourDiskButton[] swatchButtons;
        private ToolTip toolTip;
        private bool isLoading;
        private bool allowClose;

        public SettingsForm(ColourFilterForm filterForm)
        {
            this.filterForm = filterForm;

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

        private void CreateControls()
        {
            toolTip = new ToolTip();

            Label titleLabel = CreateLabel("Dyslexia Colour Filter", 28, 22, 300, 30, 15F, FontStyle.Bold);
            Controls.Add(titleLabel);

            Label introLabel = CreateLabel("Choose a comfortable tint and strength for reading.", 28, 54, 380, 22, 9F, FontStyle.Regular);
            introLabel.ForeColor = Color.FromArgb(110, 110, 115);
            Controls.Add(introLabel);

            enabledCheckBox = new CheckBox();
            enabledCheckBox.Text = "Enabled";
            enabledCheckBox.Location = new Point(28, 92);
            enabledCheckBox.AutoSize = true;
            enabledCheckBox.FlatStyle = FlatStyle.System;
            enabledCheckBox.CheckedChanged += EnabledCheckBox_CheckedChanged;
            Controls.Add(enabledCheckBox);

            Label colourLabel = CreateSectionLabel("Tint", 28, 132);
            Controls.Add(colourLabel);

            colourPreview = new Panel();
            colourPreview.Location = new Point(380, 128);
            colourPreview.Size = new Size(42, 28);
            colourPreview.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(colourPreview);

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

            colourButton = new Button();
            colourButton.Text = "Custom";
            colourButton.Location = new Point(356, 171);
            colourButton.Size = new Size(72, 32);
            StyleSecondaryButton(colourButton);
            colourButton.Click += ColourButton_Click;
            toolTip.SetToolTip(colourButton, "Choose custom colour");
            Controls.Add(colourButton);

            Label opacityLabel = CreateSectionLabel("Opacity", 28, 230);
            Controls.Add(opacityLabel);

            opacitySlider = new TrackBar();
            opacitySlider.Location = new Point(28, 256);
            opacitySlider.Size = new Size(320, 45);
            opacitySlider.Minimum = 10;
            opacitySlider.Maximum = 100;
            opacitySlider.TickFrequency = 10;
            opacitySlider.ValueChanged += OpacitySlider_ValueChanged;
            Controls.Add(opacitySlider);

            opacityValueLabel = new Label();
            opacityValueLabel.Location = new Point(366, 262);
            opacityValueLabel.Size = new Size(56, 24);
            opacityValueLabel.TextAlign = ContentAlignment.MiddleRight;
            opacityValueLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            opacityValueLabel.ForeColor = Color.FromArgb(29, 29, 31);
            Controls.Add(opacityValueLabel);

            closeButton = new Button();
            closeButton.Text = "Close";
            closeButton.Location = new Point(338, 294);
            closeButton.Size = new Size(90, 30);
            StylePrimaryButton(closeButton);
            closeButton.Click += CloseButton_Click;
            Controls.Add(closeButton);
        }

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

        private void EnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (isLoading)
            {
                return;
            }

            filterForm.SetEnabled(enabledCheckBox.Checked);
        }

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

        private void OpacitySlider_ValueChanged(object sender, EventArgs e)
        {
            UpdateOpacityLabel();

            if (isLoading)
            {
                return;
            }

            filterForm.SetOpacityPercent(opacitySlider.Value);
        }

        private void UpdateOpacityLabel()
        {
            opacityValueLabel.Text = opacitySlider.Value + "%";
        }

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

        private Label CreateSectionLabel(string text, int x, int y)
        {
            Label label = CreateLabel(text, x, y, 160, 22, 9.5F, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(72, 72, 74);
            return label;
        }

        private ColourDiskButton CreateSwatchButton(string name, Color colour, int x, int y)
        {
            ColourDiskButton button = new ColourDiskButton();
            button.AccessibleName = name;
            button.Tag = colour;
            button.DiskColour = colour;
            button.Location = new Point(x, y);
            button.Size = new Size(46, 46);
            button.Text = "";
            button.Click += SwatchButton_Click;
            toolTip.SetToolTip(button, name);
            return button;
        }

        private void StylePrimaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.FromArgb(0, 122, 255);
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

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

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Hide();
        }

        public void CloseForAppExit()
        {
            allowClose = true;
            Close();
        }

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

    public class ColourDiskButton : Button
    {
        private bool isHovered;
        private bool isSelected;
        private Color diskColour;

        public ColourDiskButton()
        {
            diskColour = Color.White;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            TabStop = true;
        }

        public Color DiskColour
        {
            get
            {
                return diskColour;
            }
            set
            {
                diskColour = value;
                Invalidate();
            }
        }

        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics graphics = pevent.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color backgroundColour = Parent == null ? Color.FromArgb(245, 245, 247) : Parent.BackColor;
            graphics.Clear(backgroundColour);

            Rectangle outer = new Rectangle(2, 2, Width - 5, Height - 5);
            Rectangle inner = new Rectangle(7, 7, Width - 15, Height - 15);

            if (isHovered || isSelected || Focused)
            {
                Color ringColour = isSelected ? Color.FromArgb(0, 122, 255) : Color.FromArgb(174, 174, 178);

                using (Pen ringPen = new Pen(ringColour, isSelected ? 3F : 2F))
                {
                    graphics.DrawEllipse(ringPen, outer);
                }
            }

            using (SolidBrush diskBrush = new SolidBrush(diskColour))
            {
                graphics.FillEllipse(diskBrush, inner);
            }

            using (Pen diskBorder = new Pen(Color.FromArgb(210, 210, 215), 1F))
            {
                graphics.DrawEllipse(diskBorder, inner);
            }
        }
    }

    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ColourFilterForm());
        }
    }
}
