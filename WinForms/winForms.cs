using System;
using System.Drawing;
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

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public ColourFilterForm()
        {
            BackColor = Color.FromArgb(255, 242, 168);
            Opacity = 0.5;

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            ShowInTaskbar = false;

            CreateTrayIcon();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            int style = GetWindowLong(Handle, GwlExStyle);
            style = style | WsExLayered | WsExTransparent | WsExToolWindow;
            SetWindowLong(Handle, GwlExStyle, style);
        }

        private void CreateTrayIcon()
        {
            enabledMenuItem = new MenuItem("Enabled", ToggleEnabled);
            enabledMenuItem.Checked = true;

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
        }

        private void SetYellow(object sender, EventArgs e)
        {
            BackColor = Color.FromArgb(255, 242, 168);
        }

        private void SetBlue(object sender, EventArgs e)
        {
            BackColor = Color.FromArgb(180, 220, 255);
        }

        private void SetGreen(object sender, EventArgs e)
        {
            BackColor = Color.FromArgb(200, 255, 200);
        }

        private void SetOpacity25(object sender, EventArgs e)
        {
            Opacity = 0.25;
        }

        private void SetOpacity50(object sender, EventArgs e)
        {
            Opacity = 0.5;
        }

        private void SetOpacity75(object sender, EventArgs e)
        {
            Opacity = 0.75;
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
