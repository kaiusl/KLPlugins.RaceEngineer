using System.Windows.Controls;

namespace KLPlugins.RaceEngineer {
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class SettingsControlDemo : UserControl {
        public RaceEngineerPlugin Plugin { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SettingsControlDemo() {
            InitializeComponent();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public SettingsControlDemo(RaceEngineerPlugin plugin) : this() {
            this.Plugin = plugin;
        }


    }
}