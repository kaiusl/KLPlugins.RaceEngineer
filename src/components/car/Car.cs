using System.Collections.Generic;
using System.IO;
using GameReaderCommon;
using Newtonsoft.Json;
using SimHub.Plugins;

namespace RaceEngineerPlugin.Car {
    public class FrontRear {
        public double F { get; set; }
        public double R { get; set; }

        public double this[int key] {
            get { 
                if (key < 2) return F;
                else return R;
            }
        }
    }

    public class TyreInfo {
        public FrontRear IdealPres { get; set; }
        public FrontRear IdealPresRange { get; set; }
        public FrontRear IdealTemp { get; set; }
        public FrontRear IdealTempRange { get; set; }
    }

    /// <summary>
    /// Class for storing information about car. Currently about tyres but can hold more.
    /// Class is designed to be initialized from .json.
    /// 
    /// <example>
    /// For example:
    /// <code>
    ///     CarInfo carInfo = JsonConvert.DeserializeObject<CarInfo>(File.ReadAllText(fname));
    /// </code>
    /// </example>
    /// 
    /// </summary>
    public class CarInfo {
        public Dictionary<string, TyreInfo> Tyres { get; set; }    
    }

    /// <summary>
    /// Store and update car related values
    /// </summary>
    public class Car {
        private const string TAG = RaceEngineerPlugin.PLUGIN_NAME + " (Car.Car): ";
        public string Name { get; private set; }
        public CarInfo Info { get; private set; }
        public CarSetup Setup { get; private set; }
        public Tyres Tyres { get; private set; }
        public Brakes Brakes { get; private set; }
        public Fuel Fuel { get; private set; }

        public Car() {
            LogInfo("Created new Car");
            Name = null;
            Info = null;
            Setup = null;
            Tyres = new Tyres();
            Brakes = new Brakes();
            Fuel = new Fuel();
        }

        public void Reset() {
            LogInfo("Car.Reset()");
            Name = null;
            Info = null;
            Setup = null;
            Fuel.Reset();
        }

        public void OnNewEvent(PluginManager pm, GameData data, Database.Database db) {
            Reset();
            CheckChange(data.NewData.CarModel);
            Fuel.OnSessionChange(pm, Name, data.NewData.TrackId, db);
        }

        public void OnNewSession(PluginManager pm, string trackName, Database.Database db) {
            Fuel.OnSessionChange(pm, Name, trackName, db);
        }

        public void OnNewStint(PluginManager pm, Database.Database db) {
            Tyres.OnNewStint(pm, db);
        }

        public void OnLapFinished(GameData data, Booleans.Booleans booleans) { 
            Tyres.OnLapFinished();
            Brakes.OnLapFinished();
            Fuel.OnLapFinished(data, booleans);
        }

        public void OnRegularUpdate(PluginManager pm, GameData data, Booleans.Booleans booleans, string trackName) {
            CheckChange(data.NewData.CarModel);
            
            if (!booleans.NewData.IsMoving && (Setup == null || (booleans.OldData.IsSetupMenuVisible && !booleans.NewData.IsSetupMenuVisible))) {
                UpdateSetup(trackName);
            }

            Brakes.CheckPadChange(pm, data);
            Tyres.CheckCompoundChange(pm, this);
            Tyres.CheckPresChange(data);

            Tyres.UpdateOverLapData(data, booleans);
            Brakes.UpdateOverLapData(data, booleans);

            Fuel.OnRegularUpdate(pm, data, booleans);

        }

        /// <summary>
        /// Check if car has changed and update accordingly
        /// </summary>
        /// <param name="newName"></param>
        /// <returns></returns>
        public bool CheckChange(string newName) {
            if (newName != null) {
                var hasChanged = Name != newName;
                if (hasChanged) {
                    LogInfo($"Car changed from '{Name}' to '{newName}'");
                    Name = newName;
                    ReadInfo();
                }

                return hasChanged;
            }
            return false;
        }

        private void ReadInfo() {
            string fname = $@"{RaceEngineerPlugin.GAME_PATH}\cars\{Name}.json";
            if (!File.Exists(fname)) {
                if (RaceEngineerPlugin.GAME.IsACC) {
                    var carClass = Name.ToLower().Contains("gt4") ? "gt4" : "gt3";
                    fname = $@"{RaceEngineerPlugin.GAME_PATH}\cars\def_{carClass}.json";
                } else {
                    fname = $@"{RaceEngineerPlugin.GAME_PATH}\cars\def.json";
                }
            }

            try {
                Info = JsonConvert.DeserializeObject<CarInfo>(File.ReadAllText(fname).Replace("\"", "'"));
                LogInfo($"Read car info from '{fname}'");
            } catch (IOException e) {
                LogInfo($"Car changed. No information file. Error: {e}");
                Info = null;
            }
        }

        public void UpdateSetup(string trackName) {
            string fname = $@"{RaceEngineerPlugin.SETTINGS.AccDataLocation}\Setups\{Name}\{trackName}\current.json";
            try {
                Setup = JsonConvert.DeserializeObject<CarSetup>(File.ReadAllText(fname).Replace("\"", "'"));
                LogInfo($"Setup changed. Read new setup from '{fname}'.");
            } catch (IOException e) {
                LogInfo($"Setup changed. But cannot read new setup. Error: {e}");
                Setup = null;
            }
        }

        private void LogInfo(string msq) {
            if (RaceEngineerPlugin.SETTINGS.Log) {
                SimHub.Logging.Current.Info(TAG + msq);
            }
        }

    }
}