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
        public string Name { get; private set; }
        public CarInfo Info { get; private set; }
        public CarSetup Setup { get; private set; }
        public Tyres Tyres { get; private set; }
        public Brakes Brakes { get; private set; }
        public Fuel Fuel { get; private set; }

        public Car() {
            RaceEngineerPlugin.LogInfo("Created new Car");
            Name = null;
            Info = null;
            Setup = null;
            Tyres = new Tyres();
            Brakes = new Brakes();
            Fuel = new Fuel();
        }

        public void Reset() {
            RaceEngineerPlugin.LogInfo("Car.Reset()");
            Name = null;
            Info = null;
            Setup = null;
            Fuel.Reset();
        }

        #region On... METHODS

        public void OnNewEvent(PluginManager pm, GameData data, Database.Database db) {
            Reset();
            CheckChange(data.NewData.CarModel);
            Brakes.OnNewEvent();
            Fuel.OnSessionChange(pm, Name, data.NewData.TrackId, db);
        }

        public void OnNewSession(PluginManager pm, string trackName, Database.Database db) {
            Fuel.OnSessionChange(pm, Name, trackName, db);
        }

        public void OnNewStint(PluginManager pm, Database.Database db) {
            Tyres.OnNewStint(pm, db);
        }

        public void OnLapFinished(GameData data, Booleans.Booleans booleans) { 
            Tyres.OnLapFinished(data.NewData.AirTemperature, data.NewData.RoadTemperature);
            Brakes.OnLapFinished();
            Fuel.OnLapFinished(data, booleans);
        }

        public void OnRegularUpdate(PluginManager pm, GameData data, Values v) {
            CheckChange(data.NewData.CarModel);
            
            if (!v.booleans.NewData.IsMoving && (Setup == null || (v.booleans.OldData.IsSetupMenuVisible && !v.booleans.NewData.IsSetupMenuVisible))) {
                UpdateSetup(data.NewData.TrackId);
            }

            Tyres.OnRegularUpdate(pm, data, v);
            Brakes.OnRegularUpdate(pm, data, v.booleans);
            Fuel.OnRegularUpdate(pm, data, v.booleans);
        }

        #endregion

        #region PRIVATE METHODS

        /// <summary>
        /// Check if car has changed and update accordingly
        /// </summary>
        /// <param name="newName"></param>
        /// <returns></returns>
        private bool CheckChange(string newName) {
            if (newName != null) {
                var hasChanged = Name != newName;
                if (hasChanged) {
                    RaceEngineerPlugin.LogInfo($"Car changed from '{Name}' to '{newName}'");
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
                RaceEngineerPlugin.LogInfo($"Read car info from '{fname}'");
            } catch (IOException e) {
                RaceEngineerPlugin.LogInfo($"Car changed. No information file. Error: {e}");
                Info = null;
            }
        }

        private void UpdateSetup(string trackName) {
            string fname = $@"{RaceEngineerPlugin.SETTINGS.AccDataLocation}\Setups\{Name}\{trackName}\current.json";
            try {
                Setup = JsonConvert.DeserializeObject<CarSetup>(File.ReadAllText(fname).Replace("\"", "'"));
                RaceEngineerPlugin.LogInfo($"Setup changed. Read new setup from '{fname}'.");
            } catch (IOException e) {
                RaceEngineerPlugin.LogInfo($"Setup changed. But cannot read new setup. Error: {e}");
                Setup = null;
            }
        }

        #endregion

    }
}