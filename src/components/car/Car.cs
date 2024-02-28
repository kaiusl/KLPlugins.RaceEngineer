using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using GameReaderCommon;

using Newtonsoft.Json;

namespace KLPlugins.RaceEngineer.Car {
    public class FrontRear<T>(T f, T r) {
        public T F { get; set; } = f;
        public T R { get; set; } = r;

        public FrontRear(T one) : this(one, one) { }
    }

    public class TyreInfo(FrontRear<double> idealPres, FrontRear<Lut> idealPresCurve, FrontRear<Lut> idealTempCurve) {
        public FrontRear<double> IdealPres { get; set; } = idealPres;
        public FrontRear<Lut> IdealPresCurve { get; set; } = idealPresCurve;
        public FrontRear<Lut> IdealTempCurve { get; set; } = idealTempCurve;


        public static TyreInfo Default() {
            var settings = RaceEngineerPlugin.Settings;
            return new TyreInfo(new FrontRear<double>(settings.IdealPres),
                new FrontRear<Lut>(settings.TyrePresNormalizationLut),
                new FrontRear<Lut>(settings.TyreTempNormalizationLut)
            );
        }

        internal static TyreInfo FromACTyreInfo(ACTyreInfo acTyreInfo) {
            var idealPres = acTyreInfo.IdealPres;

            var presCurveF = new Lut([(idealPres.F - 1.0, 0.0), (idealPres.F - 0.25, 1.0), (idealPres.F + 0.25, 1.0), (idealPres.F + 1.0, 0.0)]);
            var presCurveR = new Lut([(idealPres.R - 1.0, 0.0), (idealPres.R - 0.25, 1.0), (idealPres.R + 0.25, 1.0), (idealPres.R + 1.0, 0.0)]);

            // TODO: correctly normalize temp curves

            return new TyreInfo(
                idealPres,
                new FrontRear<Lut>(presCurveF, presCurveR),
                new FrontRear<Lut>(acTyreInfo.TempCurveF, acTyreInfo.TempCurveR)
            );
        }
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
    public class CarInfo(Dictionary<string, TyreInfo> tyres) {
        public Dictionary<string, TyreInfo> Tyres { get; set; } = tyres;

        internal void Reset() {
            this.Tyres.Clear();
        }

        internal static CarInfo FromACCarInfo(ACCarInfo acCarInfo) {
            var result = new CarInfo([]);

            foreach (var tyre in acCarInfo.Tyres) {
                result.Tyres[tyre.Key] = TyreInfo.FromACTyreInfo(tyre.Value);
            }

            return result;
        }
    }

    class FrontRearPartial<T>(T f, T r) {
        public T? F { get; set; } = f;
        public T? R { get; set; } = r;

        public FrontRear<T> Build() {
            if (this.F != null && this.R != null) {
                return new(this.F, this.R);
            } else if (this.R != null) {
                return new(this.R);
            } else if (this.F != null) {
                return new(this.F);
            } else {
                throw new Exception("Invalid JSON");
            }
        }
    }

    class TyreInfoPartial {
        public FrontRearPartial<double>? IdealPres { get; set; }
        public FrontRearPartial<Lut>? IdealPresCurve { get; set; }
        public FrontRearPartial<Lut>? IdealTempCurve { get; set; }

        public TyreInfo Build() {
            var idealPres = this.IdealPres?.Build() ?? new(RaceEngineerPlugin.Settings.IdealPres);
            var idealPresCurve = this.IdealPresCurve?.Build() ?? new(RaceEngineerPlugin.Settings.TyrePresNormalizationLut);
            var idealTempCurve = this.IdealTempCurve?.Build() ?? new(RaceEngineerPlugin.Settings.TyreTempNormalizationLut);

            return new TyreInfo(idealPres, idealPresCurve, idealTempCurve);
        }
    }

    class CarInfoPartial {
        public Dictionary<string, TyreInfoPartial>? Tyres { get; set; }

        public CarInfo Build() {
            var result = new CarInfo([]);
            if (this.Tyres != null) {
                foreach (var tyre in this.Tyres!) {
                    result.Tyres[tyre.Key] = tyre.Value.Build();
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Store and update car related values
    /// </summary>
    public class Car {
        public string? Name { get; private set; }
        public CarInfo Info { get; private set; }
        public CarSetup? Setup { get; private set; }
        public Tyres Tyres { get; private set; }
        public Brakes Brakes { get; private set; }
        public Fuel Fuel { get; private set; }

        public Car() {
            RaceEngineerPlugin.LogInfo("Created new Car");
            this.Name = null;
            this.Info = new([]);
            this.Setup = null;
            this.Tyres = new Tyres();
            this.Brakes = new Brakes();
            this.Fuel = new Fuel();
        }

        public void Reset() {
            RaceEngineerPlugin.LogInfo("Car.Reset()");
            this.Name = null;
            this.Info.Reset();
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
                var partial = JsonConvert.DeserializeObject<CarInfoPartial>(File.ReadAllText(fname).Replace("\"", "'"), new LutJsonConverter());

                if (partial != null) {
                    this.Info = partial.Build();
                } else {
                    this.Info = new([]);
                }

                RaceEngineerPlugin.LogInfo($"Read car info from '{fname}'");
            } catch (IOException) {
                //RaceEngineerPlugin.LogInfo($"Car changed. No information file. Error: {e}");
                if (RaceEngineerPlugin.Game.IsAc) {
                    var carid = data.NewData.CarId;
                    string dataPath = $@"{RaceEngineerPlugin.GameDataPath}\cars\{carid}";
                    try {
                        var acinfo = ACCarInfo.FromFile(dataPath);
                        this.Info = CarInfo.FromACCarInfo(acinfo);

                        string fnameCar = $@"{RaceEngineerPlugin.GameDataPath}\cars\{this.Name}.json";
                        if (!File.Exists(fnameCar)) {
                            File.WriteAllText(fnameCar, JsonConvert.SerializeObject(this.Info, Formatting.Indented, new LutJsonConverter()));
                        }

                        RaceEngineerPlugin.LogInfo($"Car changed. Read info from AC files: {JsonConvert.SerializeObject(this.Info, Formatting.Indented, new LutJsonConverter())}");

                    } catch (IOException e) {
                        RaceEngineerPlugin.LogInfo($"Car changed. No information file. Error: {e}");
                        this.Info.Reset();
                    }
                } else {
                    this.Info.Reset();
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

    public class WheelsData<T> {
        private Func<T> _defGenerator { get; set; }
        private T[] _data { get; set; } = new T[4];

        public WheelsData(Func<T> defGenerator) {
            this._defGenerator = defGenerator;
            for (int i = 0; i < 4; i++) {
                this._data[i] = this._defGenerator();
            }
        }

        public WheelsData(T def) : this(() => def) { }

        public void Reset() {
            for (int i = 0; i < 4; i++) {
                this._data[i] = this._defGenerator();
            }
        }

        public T FL { get => this._data[0]; set => this._data[0] = value; }
        public T FR { get => this._data[1]; set => this._data[1] = value; }
        public T RL { get => this._data[2]; set => this._data[2] = value; }
        public T RR { get => this._data[3]; set => this._data[3] = value; }

        public T this[int index] {
            get => this._data[index];
            set => this._data[index] = value;
        }
    }

    internal class ACTyreInfo(string name, string shortName, Lut wearCurveF, Lut wearCurveR, Lut tempCurveF, Lut tempCurveR, FrontRear<double> idealPres) {
        public string Name { get; private set; } = name;
        public string ShortName { get; private set; } = shortName;
        public Lut WearCurveF { get; private set; } = wearCurveF;
        public Lut WearCurveR { get; private set; } = wearCurveR;

        public FrontRear<double> IdealPres { get; private set; } = idealPres;

        public Lut TempCurveF { get; private set; } = tempCurveF;
        public Lut TempCurveR { get; private set; } = tempCurveR;
    }

    internal class ACCarInfo {
        enum FrontOrRear { F, R }
        internal class ACTyreInfoPartial {
            public string? Name { get; set; }
            public string? ShortName { get; set; }
            public Lut? WearCurveF { get; set; }
            public Lut? WearCurveR { get; set; }

            public double? IdealPresF { get; set; }
            public double? IdealPresR { get; set; }

            public Lut? TempCurveF { get; set; }
            public Lut? TempCurveR { get; set; }

            public ACTyreInfo Build() {
                return new ACTyreInfo(this.Name!, this.ShortName!, this.WearCurveF!, this.WearCurveR!, this.TempCurveF!, this.TempCurveR!, new FrontRear<double>((double)this.IdealPresF!, (double)this.IdealPresR!));
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
                                results[index].WearCurveF = Lut.FromFileAC(folder_path + value);
                                break;
                            case FrontOrRear.R:
                                results[index].WearCurveR = Lut.FromFileAC(folder_path + value);
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
                                results[index].TempCurveF = Lut.FromFileAC(folder_path + value);
                                break;
                            case FrontOrRear.R:
                                results[index].TempCurveR = Lut.FromFileAC(folder_path + value);
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

    public class Lut {
        public List<double> X { get; private set; }
        public List<double> Y { get; private set; }

        public Lut() {
            this.X = [];
            this.Y = [];
        }

        public Lut((double, double)[] values) : this() {
            for (int i = 0; i < values.Length; i++) {
                this.X.Add(values[i].Item1);
                this.Y.Add(values[i].Item2);
            }
        }

        public Lut(List<double> xs, List<double> ys) : this() {
            if (xs.Count != ys.Count) {
                throw new Exception("There must be same number of x and y values.");
            }
            this.X = xs;
            this.Y = ys;
        }


        public static Lut FromFileAC(string path) {
            return FromFile(path);
        }

        public static Lut FromFile(string path) {
            var lut = new Lut();

            var txt = File.ReadAllText(path);

            // each line is: from|to
            // optionally there are comment that start with ;
            foreach (var l in txt.Split('\n')) {
                var line = l.Trim();
                if (line == "" || line.StartsWith(";")) continue;

                var parts = line.Split('|');
                var x = Convert.ToDouble(parts[0].Trim());
                var y = Convert.ToDouble(parts[1].Split(';')[0].Trim());

                lut.X.Add(x);
                lut.Y.Add(y);
            }

            return lut;
        }
    }

    public class LutJsonConverter : JsonConverter<Lut> {
        public override void WriteJson(JsonWriter writer, Lut value, JsonSerializer serializer) {
            writer.WriteStartArray();

            for (int i = 0; i < value.X.Count; i++) {
                writer.WriteStartArray();
                writer.WriteValue(value.X[i]);
                writer.WriteValue(value.Y[i]);
                writer.WriteEndArray();
            }

            writer.WriteEndArray();
        }

        public override Lut ReadJson(JsonReader reader, Type objectType, Lut existingValue, bool hasExistingValue, JsonSerializer serializer) {
            var lut = new Lut();

            if (reader.TokenType != JsonToken.StartArray) {
                throw new Exception("Invalid JSON");
            }

            double ReadNumber(JsonReader reader) {
                reader.Read();
                return Convert.ToDouble(reader.Value);
            }

            while (reader.Read()) {
                if (reader.TokenType == JsonToken.StartArray) {
                    var x = ReadNumber(reader);
                    var y = ReadNumber(reader);

                    lut.X.Add((double)x!);
                    lut.Y.Add((double)y!);

                    reader.Read(); // eat array end
                    if (reader.TokenType != JsonToken.EndArray) {
                        throw new Exception("Invalid JSON");
                    }
                    continue;
                }

                if (reader.TokenType == JsonToken.EndArray) break;
            }

            return lut;


        }
    }
}