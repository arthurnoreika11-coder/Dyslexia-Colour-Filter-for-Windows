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
        private string selectedColour;
        private string settingsFilePath;
        private bool startEnabled;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public ColourFilterForm()
        {
            selectedColour = "Yellow";
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
            trayMenu.MenuItems.Add(enabledMenuItem);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Yellow", SetYellow);
            trayMenu.MenuItems.Add("Blue", SetBlue);
            trayMenu.MenuItems.Add("Green", SetGreen);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Opacity 25%", SetOpacity25);
            trayMenu.MenuItems.Add("Opacity 50%", SetOpacity50);
            trayMenu.MenuItems.Add("Opacity 75%", SetOpacity75);
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
            enabledMenuItem.Checked = !enabledMenuItem.Checked;
            Visible = enabledMenuItem.Checked;
            SaveSettings();
        }

        private void SetYellow(object sender, EventArgs e)
        {
            selectedColour = "Yellow";
            ApplySettings();
            SaveSettings();
        }

        private void SetBlue(object sender, EventArgs e)
        {
            selectedColour = "Blue";
            ApplySettings();
            SaveSettings();
        }

        private void SetGreen(object sender, EventArgs e)
        {
            selectedColour = "Green";
            ApplySettings();
            SaveSettings();
        }

        private void SetOpacity25(object sender, EventArgs e)
        {
            Opacity = 0.25;
            SaveSettings();
        }

        private void SetOpacity50(object sender, EventArgs e)
        {
            Opacity = 0.5;
            SaveSettings();
        }

        private void SetOpacity75(object sender, EventArgs e)
        {
            Opacity = 0.75;
            SaveSettings();
        }

        private void ApplySettings()
        {
            if (selectedColour == "Blue")
            {
                BackColor = Color.FromArgb(180, 220, 255);
            }
            else if (selectedColour == "Green")
            {
                BackColor = Color.FromArgb(200, 255, 200);
            }
            else
            {
                BackColor = Color.FromArgb(255, 242, 168);
                selectedColour = "Yellow";
            }

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
                    selectedColour = value;
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
                "Colour=" + selectedColour,
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
            trayIcon.Visible = false;
            trayIcon.Dispose();

            base.OnFormClosed(e);
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
