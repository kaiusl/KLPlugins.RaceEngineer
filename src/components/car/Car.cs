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

        public void OnNewEvent(PluginManager pm, GameData data, int trackGrip, Database.Database db) {
            CheckChange(data.NewData.CarModel);
            Fuel.OnNewEvent(Name, data.NewData.TrackId, trackGrip, db);
        }

        public void OnNewSession(PluginManager pm, string trackName, int trackGrip, Database.Database db) {
            Fuel.OnSessionChange(pm, Name, trackName, trackGrip, db);
        }

        public void OnNewStint(PluginManager pm, Database.Database db) {
            Tyres.OnNewStint(pm, db);
        }

        public void OnLapFinished(PluginManager pm, GameData data, Booleans.Booleans booleans) { 
            Tyres.OnLapFinished(pm, data.NewData.AirTemperature, data.NewData.RoadTemperature);
            Brakes.OnLapFinished();
            Fuel.OnLapFinished(data, booleans);
        }

        public void OnLapFinishedAfterInsert() {
            Tyres.OnLapFinishedAfterInsert();
        }

        public void OnRegularUpdate(PluginManager pm, GameData data, Values v) {
            //Stopwatch sw2 = Stopwatch.StartNew();
            //Stopwatch sw = Stopwatch.StartNew();

            CheckChange(data.NewData.CarModel);
            //sw.Stop();
            //sw2.Stop();
            //var ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\timings\\RETiming_Car_CheckChange_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //sw2.Start();
            //sw.Restart();
            if (!v.booleans.NewData.IsMoving && (Setup == null || (v.booleans.OldData.IsSetupMenuVisible && !v.booleans.NewData.IsSetupMenuVisible))) {
                UpdateSetup(data.NewData.TrackId);
            }
            //sw.Stop();
            //sw2.Stop();
            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\timings\\RETiming_Car_UpdateSetup_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //sw2.Start();
            //sw.Restart();
            Tyres.OnRegularUpdate(pm, data, v);
            //sw.Stop();
            //sw2.Stop();
            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\timings\\RETiming_Car_Tyres_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //sw2.Start();
            //sw.Restart();
            Brakes.OnRegularUpdate(pm, data, v.booleans);
            //sw.Stop();
            //sw2.Stop();
            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\timings\\RETiming_Car_Brakes_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //sw2.Start();
            //sw.Restart();
            Fuel.OnRegularUpdate(pm, data, v.booleans);
            //sw.Stop();
            //sw2.Stop();
            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\timings\\RETiming_Car_Fuel_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");
            //ts = sw2.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\timings\\RETiming_Car_total_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");
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