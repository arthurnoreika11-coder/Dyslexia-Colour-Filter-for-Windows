using system;

namespace LearningConsole
{
    public class FilterSettings
    {
        public string Colour { get; set; }
        public string Opacity { get; set; }
        public string isEnabled { get; set; }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            FilterSettings settings = new
            FilterSettings();

            settings.Colour = "#FFF2A8";
            settings.Opacity = "0.5";
            settings.isEnabled = "true";

            LearningConsole.WriteLine("Dyslexia Filter Settings:");
            LearningConsole.WriteLine($"Colour: {settings.Colour}");
            LearningConsole.WriteLine($"Opacity: {settings.Opacity}");
            LearningConsole.WriteLine($"Enabled: {settings.isEnabled}");

            LearningConsole.ReadLine();
        }
    }
}