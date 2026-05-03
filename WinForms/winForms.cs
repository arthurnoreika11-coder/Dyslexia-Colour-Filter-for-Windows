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
        private Panel colourPreview;
        private TrackBar opacitySlider;
        private Label opacityValueLabel;
        private bool isLoading;
        private bool allowClose;

        public SettingsForm(ColourFilterForm filterForm)
        {
            this.filterForm = filterForm;

            Text = "Dyslexia Colour Filter Settings";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(330, 190);

            CreateControls();
            LoadCurrentSettings();
        }

        private void CreateControls()
        {
            enabledCheckBox = new CheckBox();
            enabledCheckBox.Text = "Enabled";
            enabledCheckBox.Location = new Point(20, 20);
            enabledCheckBox.AutoSize = true;
            enabledCheckBox.CheckedChanged += EnabledCheckBox_CheckedChanged;
            Controls.Add(enabledCheckBox);

            Label colourLabel = new Label();
            colourLabel.Text = "Colour";
            colourLabel.Location = new Point(20, 58);
            colourLabel.AutoSize = true;
            Controls.Add(colourLabel);

            colourPreview = new Panel();
            colourPreview.Location = new Point(85, 55);
            colourPreview.Size = new Size(40, 24);
            colourPreview.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(colourPreview);

            colourButton = new Button();
            colourButton.Text = "Choose...";
            colourButton.Location = new Point(140, 52);
            colourButton.Size = new Size(90, 30);
            colourButton.Click += ColourButton_Click;
            Controls.Add(colourButton);

            Label opacityLabel = new Label();
            opacityLabel.Text = "Opacity";
            opacityLabel.Location = new Point(20, 105);
            opacityLabel.AutoSize = true;
            Controls.Add(opacityLabel);

            opacitySlider = new TrackBar();
            opacitySlider.Location = new Point(85, 95);
            opacitySlider.Size = new Size(170, 45);
            opacitySlider.Minimum = 10;
            opacitySlider.Maximum = 100;
            opacitySlider.TickFrequency = 10;
            opacitySlider.ValueChanged += OpacitySlider_ValueChanged;
            Controls.Add(opacitySlider);

            opacityValueLabel = new Label();
            opacityValueLabel.Location = new Point(265, 105);
            opacityValueLabel.Size = new Size(50, 20);
            Controls.Add(opacityValueLabel);

            Button closeButton = new Button();
            closeButton.Text = "Close";
            closeButton.Location = new Point(220, 145);
            closeButton.Size = new Size(90, 30);
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
