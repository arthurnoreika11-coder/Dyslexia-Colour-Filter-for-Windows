using System;

namespace LearningConsole
{
    public class FilterSettings
    {
        public FilterSettings()
        {
            Colour = string.Empty;
            Opacity = string.Empty;
            isEnabled = string.Empty;
        }

        public string Colour { get; set; }
        public string Opacity { get; set; }
        public string isEnabled { get; set; }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            FilterSettings settings = new FilterSettings();

            settings.Colour = "#FFF2A8";
            settings.Opacity = "0.5";
            settings.isEnabled = "true";

            Console.WriteLine("Dyslexia Filter Settings:");
            Console.WriteLine("Colour: " + settings.Colour);
            Console.WriteLine("Opacity: " + settings.Opacity);
            Console.WriteLine("Enabled: " + settings.isEnabled);

            Console.ReadLine();
        }
    }
}
