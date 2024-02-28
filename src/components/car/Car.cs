using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;

using GameReaderCommon;

using Newtonsoft.Json;

namespace KLPlugins.RaceEngineer.Car {
    public class FrontRear(double f, double r) {
        public double F { get; set; } = f;
        public double R { get; set; } = r;

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
        internal ACCarInfo? ACInfo { get; private set; }
        public CarSetup? Setup { get; private set; }
        public Tyres Tyres { get; private set; }
        public Brakes Brakes { get; private set; }
        public Fuel Fuel { get; private set; }

        public Car() {
            RaceEngineerPlugin.LogInfo("Created new Car");
            this.Name = null;
            this.Info = null;
            this.ACInfo = null;
            this.Setup = null;
            this.Tyres = new Tyres();
            this.Brakes = new Brakes();
            this.Fuel = new Fuel();
        }

        public void Reset() {
            RaceEngineerPlugin.LogInfo("Car.Reset()");
            this.Name = null;
            this.Info = null;
            this.ACInfo = null;
            this.Setup = null;
            this.Tyres.Reset();
            this.Brakes.Reset();
            this.Fuel.Reset();
        }

        #region On... METHODS

        public void OnNewEvent(GameData data, Values v) {
            this.CheckChange(data);
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
            this.CheckChange(data);

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
        private bool CheckChange(GameData data) {
            var newName = data.NewData.CarModel;
            if (newName != null) {
                var hasChanged = this.Name != newName;
                if (hasChanged) {
                    RaceEngineerPlugin.LogInfo($"Car changed from '{this.Name}' to '{newName}'");
                    this.Name = newName;
                    this.ReadInfo(data);
                }

                return hasChanged;
            }
            return false;
        }

        private void ReadInfo(GameData data) {
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
                if (RaceEngineerPlugin.Game.IsAc) {
                    var carid = data.NewData.CarId;
                    string dataPath = $@"{RaceEngineerPlugin.GameDataPath}\cars\{carid}";
                    try {
                        this.ACInfo = ACCarInfo.FromFile(dataPath);
                    } catch (IOException e) {
                        RaceEngineerPlugin.LogInfo($"Car changed. No information file. Error: {e}");
                        this.Info = null;
                        this.ACInfo = null;
                    }
                } else {
                    this.Info = null;
                    this.ACInfo = null;
                }

            }
        }

        private void UpdateSetup(string trackName) {
            // TODO: this should work for other games too, if we implement their setup structure
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

    internal class ACTyreInfo(string name, string shortName, ACLut wearCurveF, ACLut wearCurveR, ACLut tempCurveF, ACLut tempCurveR, FrontRear idealPres) {
        public string Name { get; private set; } = name;
        public string ShortName { get; private set; } = shortName;
        public ACLut WearCurveF { get; private set; } = wearCurveF;
        public ACLut WearCurveR { get; private set; } = wearCurveR;

        public FrontRear IdealPres { get; private set; } = idealPres;

        public ACLut TempCurveF { get; private set; } = tempCurveF;
        public ACLut TempCurveR { get; private set; } = tempCurveR;
    }

    internal class ACCarInfo {
        enum FrontOrRear { F, R }
        internal class ACTyreInfoPartial {
            public string? Name { get; set; }
            public string? ShortName { get; set; }
            public ACLut? WearCurveF { get; set; }
            public ACLut? WearCurveR { get; set; }

            public double? IdealPresF { get; set; }
            public double? IdealPresR { get; set; }

            public ACLut? TempCurveF { get; set; }
            public ACLut? TempCurveR { get; set; }

            public ACTyreInfo Build() {
                return new ACTyreInfo(this.Name!, this.ShortName!, this.WearCurveF!, this.WearCurveR!, this.TempCurveF!, this.TempCurveR!, new FrontRear((double)this.IdealPresF!, (double)this.IdealPresR!));
            }
        }

        public Dictionary<string, ACTyreInfo> Tyres { get; private set; } = [];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"> Path to a folder containing the tyres.ini file and other LUTs.</param>
        /// <returns></returns>
        public static ACCarInfo FromFile(string path) {
            var info = new ACCarInfo();
            Dictionary<int, ACTyreInfoPartial> results = [];

            var folder_path = path + "\\";
            var tyresini_path = folder_path + "tyres.ini";
            var txt = File.ReadAllText(tyresini_path);

            FrontOrRear? frontOrRear = null;
            int index = -1;

            foreach (var l in txt.Split('\n')) {
                var line = l.Trim();
                var line_lower = l.ToLower();
                if (line == "" || line.StartsWith(";")) continue;

                if (line.StartsWith("[")) {
                    if (line_lower.StartsWith("[front") || line_lower.StartsWith("[thermal_front")) {
                        frontOrRear = FrontOrRear.F;
                    } else if (line_lower.StartsWith("[rear") || line_lower.StartsWith("[thermal_rear")) {
                        frontOrRear = FrontOrRear.R;
                    } else {
                        continue;
                    }

                    var splits = line.Split('_');
                    try {
                        // this fails for [front], [thermal_front], index is 0 then, otherwise [front_2], [thermal_front_3] have indexes
                        var indexStr = splits.Last().Split(']')[0];
                        SimHub.Logging.Current.Info(indexStr);
                        index = Convert.ToInt32(indexStr);
                    } catch {
                        index = 0;
                    }

                    if (!results.ContainsKey(index)) {
                        results[index] = new ACTyreInfoPartial();
                    }
                    continue;
                }

                var parts = line.Split('=');
                var key = parts[0].Trim();
                var value = parts[1].Split(';')[0].Trim(); // remove trailing comments and spaces

                switch (key.ToLower()) {
                    case "name":
                        results[index].Name = value;
                        break;
                    case "short_name":
                        results[index].ShortName = value;
                        break;
                    case "wear_curve":
                        switch (frontOrRear) {
                            case FrontOrRear.F:
                                results[index].WearCurveF = ACLut.FromFile(folder_path + value);
                                break;
                            case FrontOrRear.R:
                                results[index].WearCurveR = ACLut.FromFile(folder_path + value);
                                break;
                        }
                        break;
                    case "pressure_ideal":
                        switch (frontOrRear) {
                            case FrontOrRear.F:
                                results[index].IdealPresF = Convert.ToDouble(value);
                                break;
                            case FrontOrRear.R:
                                results[index].IdealPresR = Convert.ToDouble(value);
                                break;
                        }
                        break;
                    case "performance_curve":
                        switch (frontOrRear) {
                            case FrontOrRear.F:
                                results[index].TempCurveF = ACLut.FromFile(folder_path + value);
                                break;
                            case FrontOrRear.R:
                                results[index].TempCurveR = ACLut.FromFile(folder_path + value);
                                break;
                        }
                        break;
                    default:
                        break;
                }

            }

            foreach (var r in results.Values) {
                if (r.Name != null) {
                    info.Tyres[r.Name] = r.Build();
                }
            }

            return info;
        }
    }

    internal class ACLut {
        public List<Tuple<double, double>> Values { get; private set; } = new();

        public static ACLut FromFile(string path) {
            var lut = new ACLut();

            var txt = File.ReadAllText(path);

            // each line is: from|to
            // optionally there are comment that start with ;
            foreach (var l in txt.Split('\n')) {
                var line = l.Trim();
                if (line == "" || line.StartsWith(";")) continue;

                var parts = line.Split('|');
                var from = Convert.ToDouble(parts[0].Trim());
                var to = Convert.ToDouble(parts[1].Split(';')[0].Trim());

                lut.Values.Add(new Tuple<double, double>(from, to));
            }

            return lut;
        }
    }
}