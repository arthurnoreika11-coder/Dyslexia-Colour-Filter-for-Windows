using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinForms
{
    public class ColourFilterForm : Form
    {
        public ColourFilterForm()
        {
            BackColor = Color.FromArgb(255, 242, 168);
            Opacity = 0.5;

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            ShowInTaskbar = false;
            KeyPreview = true;

            KeyDown += ColourFilterForm_KeyDown;
        }

        private void ColourFilterForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
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
