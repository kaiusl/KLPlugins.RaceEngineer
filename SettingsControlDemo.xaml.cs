using System.Windows.Controls;

namespace KLPlugins.RaceEngineer {
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class SettingsControlDemo : UserControl {
        public RaceEngineerPlugin Plugin { get; }

        public SettingsControlDemo() {
            InitializeComponent();
        }

        public SettingsControlDemo(RaceEngineerPlugin plugin) : this() {
            this.Plugin = plugin;
        }


    }
}