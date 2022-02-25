namespace RaceEngineerPlugin.Game {
    public class Game {
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
        public string Name { get => _name; }

        private bool _isAC = false;
        private bool _isACC = false;
        private bool _isRf2 = false;
        private bool _isIracing = false;
        private bool _isR3E = false;
        private string _name;

        public Game(string gameName) {
            Update(gameName);
        }

        private void Update(string gameName) {
            _name = gameName;
            switch (gameName) { 
                case AC_NAME :
                    _isAC = true;
                    break;
                case ACC_NAME :
                    _isACC = true;
                    break;
                case RF2_NAME :
                    _isRf2 = true;
                    break;
                case IRACING_NAME :
                    _isIracing = true;
                    break;
                case R3E_NAME :
                    _isR3E = true;
                    break;
            }
        }

    }
}