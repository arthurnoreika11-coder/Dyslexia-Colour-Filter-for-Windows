using System;
using System.Drawing;
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
        private Button[] swatchButtons;
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
            ClientSize = new Size(430, 320);
            BackColor = Color.FromArgb(248, 249, 251);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            TopMost = true;

            CreateControls();
            LoadCurrentSettings();
        }

        private void CreateControls()
        {
            toolTip = new ToolTip();

            Label titleLabel = CreateLabel("Colour filter", 24, 20, 220, 28, 14F, FontStyle.Bold);
            Controls.Add(titleLabel);

            Label introLabel = CreateLabel("Choose a comfortable tint and strength for reading.", 24, 50, 360, 22, 9F, FontStyle.Regular);
            introLabel.ForeColor = Color.FromArgb(88, 95, 105);
            Controls.Add(introLabel);

            enabledCheckBox = new CheckBox();
            enabledCheckBox.Text = "Enabled";
            enabledCheckBox.Location = new Point(24, 86);
            enabledCheckBox.AutoSize = true;
            enabledCheckBox.FlatStyle = FlatStyle.System;
            enabledCheckBox.CheckedChanged += EnabledCheckBox_CheckedChanged;
            Controls.Add(enabledCheckBox);

            Label colourLabel = CreateSectionLabel("Colour", 24, 122);
            Controls.Add(colourLabel);

            colourPreview = new Panel();
            colourPreview.Location = new Point(346, 117);
            colourPreview.Size = new Size(48, 30);
            colourPreview.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(colourPreview);

            Button creamSwatch = CreateSwatchButton("Cream", Color.FromArgb(255, 242, 168), 24, 152);
            Button pinkSwatch = CreateSwatchButton("Pink", Color.FromArgb(255, 215, 229), 88, 152);
            Button blueSwatch = CreateSwatchButton("Blue", Color.FromArgb(180, 220, 255), 152, 152);
            Button mintSwatch = CreateSwatchButton("Mint", Color.FromArgb(200, 255, 200), 216, 152);
            Button greySwatch = CreateSwatchButton("Grey", Color.FromArgb(216, 221, 230), 280, 152);

            swatchButtons = new Button[]
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
            colourButton.Location = new Point(344, 152);
            colourButton.Size = new Size(62, 34);
            StyleSecondaryButton(colourButton);
            colourButton.Click += ColourButton_Click;
            toolTip.SetToolTip(colourButton, "Choose custom colour");
            Controls.Add(colourButton);

            Label opacityLabel = CreateSectionLabel("Opacity", 24, 214);
            Controls.Add(opacityLabel);

            opacitySlider = new TrackBar();
            opacitySlider.Location = new Point(24, 240);
            opacitySlider.Size = new Size(300, 45);
            opacitySlider.Minimum = 10;
            opacitySlider.Maximum = 100;
            opacitySlider.TickFrequency = 10;
            opacitySlider.ValueChanged += OpacitySlider_ValueChanged;
            Controls.Add(opacitySlider);

            opacityValueLabel = new Label();
            opacityValueLabel.Location = new Point(338, 246);
            opacityValueLabel.Size = new Size(56, 24);
            opacityValueLabel.TextAlign = ContentAlignment.MiddleRight;
            opacityValueLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            opacityValueLabel.ForeColor = Color.FromArgb(49, 56, 66);
            Controls.Add(opacityValueLabel);

            closeButton = new Button();
            closeButton.Text = "Close";
            closeButton.Location = new Point(316, 282);
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
            label.ForeColor = Color.FromArgb(34, 40, 49);
            return label;
        }

        private Label CreateSectionLabel(string text, int x, int y)
        {
            Label label = CreateLabel(text, x, y, 160, 22, 9.5F, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(58, 65, 75);
            return label;
        }

        private Button CreateSwatchButton(string name, Color colour, int x, int y)
        {
            Button button = new Button();
            button.AccessibleName = name;
            button.Tag = colour;
            button.Location = new Point(x, y);
            button.Size = new Size(48, 34);
            button.BackColor = colour;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 2;
            button.FlatAppearance.BorderColor = Color.FromArgb(205, 211, 220);
            button.Text = "";
            button.Cursor = Cursors.Hand;
            button.Click += SwatchButton_Click;
            button.MouseEnter += SwatchButton_MouseEnter;
            button.MouseLeave += SwatchButton_MouseLeave;
            toolTip.SetToolTip(button, name);
            return button;
        }

        private void StylePrimaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.FromArgb(39, 95, 118);
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        private void StyleSecondaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(39, 95, 118);
            button.FlatAppearance.BorderColor = Color.FromArgb(181, 194, 204);
            button.FlatAppearance.BorderSize = 1;
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        private void SwatchButton_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button == null)
            {
                return;
            }

            Color colour = (Color)button.Tag;
            colourPreview.BackColor = colour;
            filterForm.SetFilterColour(colour);
            UpdateSelectedSwatch();
        }

        private void SwatchButton_MouseEnter(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button != null && button.FlatAppearance.BorderColor != Color.FromArgb(39, 95, 118))
            {
                button.FlatAppearance.BorderColor = Color.FromArgb(135, 151, 164);
            }
        }

        private void SwatchButton_MouseLeave(object sender, EventArgs e)
        {
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

                if (swatchColour.ToArgb() == selected.ToArgb())
                {
                    swatchButtons[i].FlatAppearance.BorderColor = Color.FromArgb(39, 95, 118);
                    swatchButtons[i].FlatAppearance.BorderSize = 3;
                }
                else
                {
                    swatchButtons[i].FlatAppearance.BorderColor = Color.FromArgb(205, 211, 220);
                    swatchButtons[i].FlatAppearance.BorderSize = 2;
                }
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
