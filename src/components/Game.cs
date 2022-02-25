namespace RaceEngineerPlugin.Game {


    /// <summary>
    /// Booleans to tell which game we have. Since different games have different available data then we need to do alot of check like gameName == "...".
    /// The gameName is constant in each plugin reload and thus we can set it once and simplyfy game checks alot.
    /// </summary>
    public class Game {
        private const string TAG = RaceEngineerPlugin.PLUGIN_NAME + " (Game): ";
        public const string AC_NAME = "AssettoCorsa";
        public const string ACC_NAME = "AssettoCorsaCompetizione";
        public const string RF2_NAME = "RFactor2";
        public const string IRACING_NAME = "IRacing";
        public const string R3E_NAME = "RRRE";

        public bool IsAC { get => _isAC; }
        public bool IsACC { get => _isACC; }
        public bool IsRf2 { get => _isRf2; }
        public bool IsIracing { get => _isIracing; }
        public bool IsR3E { get => _isR3E; }
        public bool IsUnknown { get => _isUnknown;  }
        public string Name { get => _name; }

        private bool _isAC = false;
        private bool _isACC = false;
        private bool _isRf2 = false;
        private bool _isIracing = false;
        private bool _isR3E = false;
        private bool _isUnknown = false;
        private string _name;

        public Game(string gameName) {
            Update(gameName);
        }

        private void Update(string gameName) {
            _name = gameName;
            switch (gameName) { 
                case AC_NAME :
                    _isAC = true;
                    LogInfo("Game set to AC");
                    break;
                case ACC_NAME :
                    _isACC = true;
                    LogInfo("Game set to ACC");
                    break;
                case RF2_NAME :
                    _isRf2 = true;
                    LogInfo("Game set to RF2");
                    break;
                case IRACING_NAME :
                    _isIracing = true;
                    LogInfo("Game set to IRacing");
                    break;
                case R3E_NAME :
                    _isR3E = true;
                    LogInfo("Game set to R3E");
                    break;
                default :
                    _isUnknown = true;
                    LogInfo("Game set to Unknown");
                    break;
            }


        }

        private void LogInfo(string msq) {
            if (RaceEngineerPlugin.SETTINGS.Log) {
                SimHub.Logging.Current.Info(TAG + msq);
            }
        }

    }
}