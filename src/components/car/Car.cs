using System.Collections.Generic;
using System.IO;

using GameReaderCommon;

using Newtonsoft.Json;

namespace KLPlugins.RaceEngineer.Car {
    public class FrontRear {
        public double F { get; set; }
        public double R { get; set; }

        public double this[int key] {
            get {
                if (key < 2) {
                    return this.F;
                } else {
                    return this.R;
                }
            }
        }
    }

    public class TyreInfo {
        public FrontRear? IdealPres { get; set; }
        public FrontRear? IdealPresRange { get; set; }
        public FrontRear? IdealTemp { get; set; }
        public FrontRear? IdealTempRange { get; set; }
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
        public Dictionary<string, TyreInfo>? Tyres { get; set; }
    }

    /// <summary>
    /// Store and update car related values
    /// </summary>
    public class Car {
        public string? Name { get; private set; }
        public CarInfo? Info { get; private set; }
        public CarSetup? Setup { get; private set; }
        public Tyres Tyres { get; private set; }
        public Brakes Brakes { get; private set; }
        public Fuel Fuel { get; private set; }

        public Car() {
            RaceEngineerPlugin.LogInfo("Created new Car");
            this.Name = null;
            this.Info = null;
            this.Setup = null;
            this.Tyres = new Tyres();
            this.Brakes = new Brakes();
            this.Fuel = new Fuel();
        }

        public void Reset() {
            RaceEngineerPlugin.LogInfo("Car.Reset()");
            this.Name = null;
            this.Info = null;
            this.Setup = null;
            this.Tyres.Reset();
            this.Brakes.Reset();
            this.Fuel.Reset();
        }

        #region On... METHODS

        public void OnNewEvent(GameData data, Values v) {
            this.CheckChange(data.NewData.CarModel);
            this.Fuel.OnNewEvent(data, v);
        }

        public void OnNewSession(GameData data, Values v) {
            this.Fuel.OnSessionChange(data, v);
        }

        public void OnNewStint() {
            this.Tyres.OnNewStint();
        }

        public void OnLapFinished(GameData data, Values v) {
            this.Tyres.OnLapFinished(v);
            this.Brakes.OnLapFinished(v);
            this.Fuel.OnLapFinished(data, v);
        }

        public void OnLapFinishedAfterInsert() {
            this.Tyres.OnLapFinishedAfterInsert();
        }

        public void OnRegularUpdate(GameData data, Values v) {
            this.CheckChange(data.NewData.CarModel);

            if (RaceEngineerPlugin.Game.IsAcc && !v.Booleans.NewData.IsMoving && (this.Setup == null || (v.Booleans.OldData.IsSetupMenuVisible && !v.Booleans.NewData.IsSetupMenuVisible))) {
                this.UpdateSetup(data.NewData.TrackId);
            }

            this.Tyres.OnRegularUpdate(data, v);
            this.Brakes.OnRegularUpdate(data, v);
            this.Fuel.OnRegularUpdate(data, v);
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
                var hasChanged = this.Name != newName;
                if (hasChanged) {
                    RaceEngineerPlugin.LogInfo($"Car changed from '{this.Name}' to '{newName}'");
                    this.Name = newName;
                    this.ReadInfo();
                }

                return hasChanged;
            }
            return false;
        }

        private void ReadInfo() {
            if (this.Name == null) return;


            string fname = $@"{RaceEngineerPlugin.GameDataPath}\cars\{this.Name}.json";
            if (!File.Exists(fname)) {
                if (RaceEngineerPlugin.Game.IsAcc) {
                    var carClass = Helpers.GetAccCarClass(this.Name);
                    fname = $@"{RaceEngineerPlugin.GameDataPath}\cars\{carClass}.json";
                    if (!File.Exists(fname)) {
                        fname = $@"{RaceEngineerPlugin.GameDataPath}\cars\def.json";
                    }
                } else {
                    fname = $@"{RaceEngineerPlugin.GameDataPath}\cars\def.json";
                }
            }

            try {
                this.Info = JsonConvert.DeserializeObject<CarInfo>(File.ReadAllText(fname).Replace("\"", "'"));
                RaceEngineerPlugin.LogInfo($"Read car info from '{fname}'");
            } catch (IOException) {
                //RaceEngineerPlugin.LogInfo($"Car changed. No information file. Error: {e}");
                this.Info = null;
            }
        }

        private void UpdateSetup(string trackName) {
            string fname = $@"{RaceEngineerPlugin.Settings.AccDataLocation}\Setups\{this.Name}\{trackName}\current.json";
            try {
                this.Setup = JsonConvert.DeserializeObject<CarSetup>(File.ReadAllText(fname).Replace("\"", "'"));
                RaceEngineerPlugin.LogInfo($"Setup changed. Read new setup from '{fname}'.");
                this.Tyres.OnSetupChange();
            } catch (IOException e) {
                //RaceEngineerPlugin.LogInfo($"Setup changed. But cannot read new setup. Error: {e}");
                this.Setup = null;
            }
        }

        #endregion

    }
}