using System.Collections.Generic;
using System.Diagnostics;
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
            Tyres.Reset();
            Brakes.Reset();
            Fuel.Reset();
        }

        #region On... METHODS

        public void OnNewEvent(GameData data, Values v) {
            CheckChange(data.NewData.CarModel);
            Fuel.OnNewEvent(v);
        }

        public void OnNewSession(Values v) {
            Fuel.OnSessionChange(v);
        }

        public void OnNewStint() {
            Tyres.OnNewStint();
        }

        public void OnLapFinished(GameData data, Values v) { 
            Tyres.OnLapFinished(data.NewData.AirTemperature, data.NewData.RoadTemperature);
            Brakes.OnLapFinished();
            Fuel.OnLapFinished(data, v);
        }

        public void OnLapFinishedAfterInsert() {
            Tyres.OnLapFinishedAfterInsert();
        }

        public void OnRegularUpdate(GameData data, Values v) {
            CheckChange(data.NewData.CarModel);
           
            if (!v.Booleans.NewData.IsMoving && (Setup == null || (v.Booleans.OldData.IsSetupMenuVisible && !v.Booleans.NewData.IsSetupMenuVisible))) {
                UpdateSetup(data.NewData.TrackId);
            }
            
            Tyres.OnRegularUpdate(data, v);
            Brakes.OnRegularUpdate(data, v);
            Fuel.OnRegularUpdate(data, v);
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
            string fname = $@"{RaceEngineerPlugin.GameDataPath}\cars\{Name}.json";
            if (!File.Exists(fname)) {
                if (RaceEngineerPlugin.Game.IsAcc) {
                    var carClass = Name.ToLower().Contains("gt4") ? "gt4" : "gt3";
                    fname = $@"{RaceEngineerPlugin.GameDataPath}\cars\def_{carClass}.json";
                } else {
                    fname = $@"{RaceEngineerPlugin.GameDataPath}\cars\def.json";
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
            string fname = $@"{RaceEngineerPlugin.Settings.AccDataLocation}\Setups\{Name}\{trackName}\current.json";
            try {
                Setup = JsonConvert.DeserializeObject<CarSetup>(File.ReadAllText(fname).Replace("\"", "'"));
                RaceEngineerPlugin.LogInfo($"Setup changed. Read new setup from '{fname}'.");
                Tyres.OnSetupChange();
            } catch (IOException e) {
                RaceEngineerPlugin.LogInfo($"Setup changed. But cannot read new setup. Error: {e}");
                Setup = null;
            }
        }

        #endregion

    }
}