using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using GameReaderCommon;

using KLPlugins.RaceEngineer.Stats;

using MathNet.Numerics.LinearAlgebra.Factorization;

using Newtonsoft.Json;

namespace KLPlugins.RaceEngineer.Car {
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

        internal Car() {
            RaceEngineerPlugin.LogInfo("Created new Car");
            this.Name = null;
            this.Info = new([]);
            this.Setup = null;
            this.Tyres = new Tyres();
            this.Brakes = new Brakes();
            this.Fuel = new Fuel();
        }

        internal void Reset() {
            RaceEngineerPlugin.LogInfo("Car.Reset()");
            this.Name = null;
            this.Info.Reset();
            this.Setup = null;
            this.Tyres.Reset();
            this.Brakes.Reset();
            this.Fuel.Reset();
        }

        #region On... METHODS

        internal void OnNewEvent(GameData data, Values v) {
            this.CheckChange(data);
            this.Fuel.OnNewEvent(data, v);
        }

        internal void OnNewSession(GameData data, Values v) {
            this.Fuel.OnSessionChange(data, v);
        }

        internal void OnNewStint() {
            this.Tyres.OnNewStint();
        }

        internal void OnLapFinished(GameData data, Values v) {
            this.Tyres.OnLapFinished(v);
            this.Brakes.OnLapFinished(v);
            this.Fuel.OnLapFinished(data, v);
        }

        internal void OnLapFinishedAfterInsert() {
            this.Tyres.OnLapFinishedAfterInsert();
        }

        internal void OnRegularUpdate(GameData data, Values v) {
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
            var newName = data.NewData.CarId;
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
            this.Info.Reset();

            if (RaceEngineerPlugin.Game.IsAc) {
                this.ReadInfoAC(data);
                return;
            }

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
                    this.Info = CarInfo.FromPartial(partial);
                }

                RaceEngineerPlugin.LogInfo($"Read car info from '{fname}'");
            } catch (IOException) {
                //
            }
        }

        private void ReadInfoAC(GameData data) {
            if (this.Name == null) return;
            if (!RaceEngineerPlugin.Game.IsAc) throw new Exception("ReadInfoAC called when not AC game");

            var carid = data.NewData.CarId;
            string pluginsCarDataPath = $@"{RaceEngineerPlugin.GameDataPath}\cars\{this.Name}.json";
            string ACRawDataPath = $@"{RaceEngineerPlugin.GameDataPath}\cars\{carid}";

            CarInfoPartial partialInfo = new();

            // 1. Try to read car specific data file
            try {
                var txt = File.ReadAllText(pluginsCarDataPath).Replace("\"", "'");
                var partial = JsonConvert.DeserializeObject<CarInfoPartial>(txt, new LutJsonConverter());

                if (partial != null) {
                    partialInfo = partial;
                }
            } catch (IOException) {
                //
            }

            if (partialInfo.IsFullyInitializedAC()) {
                RaceEngineerPlugin.LogInfo($"Read complete car info from '{pluginsCarDataPath}'");

                // got all the date we need
                this.Info = CarInfo.FromPartial(partialInfo);
                return;
            }

            // 2. Car specific file was not present or partially initialized. Try reading the AC's raw data files
            try {
                var acinfo = ACCarInfo.FromFile(ACRawDataPath);
                // if we didn't throw then acinfo does contain all required data
                this.Info = CarInfo.FromPartialAndACData(partialInfo, acinfo);

                // Write the data out, so that we don't need to go through it next time
                var json = JsonConvert.SerializeObject(this.Info, Formatting.Indented, new LutJsonConverter(), new FrontRearJsonConverter<double>(), new FrontRearJsonConverter<Lut>());
                File.WriteAllText(pluginsCarDataPath, json);

                RaceEngineerPlugin.LogInfo($"Read partial car info from '{pluginsCarDataPath}'. Filled the gaps from AC's raw files. Wrote out '{pluginsCarDataPath}'.");

                return;
            } catch (IOException) {
                //
            }

            // 3. Didn't find AC raw data files, try to read def.json to fill in the gaps
            var defDataPath = $@"{RaceEngineerPlugin.GameDataPath}\cars\def.json";
            try {
                var txt = File.ReadAllText(defDataPath).Replace("\"", "'");
                var partial = JsonConvert.DeserializeObject<CarInfoPartial>(txt, new LutJsonConverter());

                if (partial != null) {
                    RaceEngineerPlugin.LogInfo($"Read car info from '{pluginsCarDataPath}'. Filled the gaps from def file '{defDataPath}'.");
                    partialInfo.FillGaps(partial);
                }
            } catch (IOException) {
                //
            }

            // 4. Fill the remaining gaps with default settings
            this.Info = CarInfo.FromPartial(partialInfo);
        }

        private void UpdateSetup(string trackName) {
            // TODO: this should work for other games too, if we implement their setup structure
            string fname = $@"{RaceEngineerPlugin.Settings.AccDataLocation}\Setups\{this.Name}\{trackName}\current.json";
            try {
                this.Setup = JsonConvert.DeserializeObject<CarSetup>(File.ReadAllText(fname).Replace("\"", "'"));
                RaceEngineerPlugin.LogInfo($"Setup changed. Read new setup from '{fname}'.");
                RaceEngineerPlugin.LogInfo($"New setup is:\n{JsonConvert.SerializeObject(this.Setup, Formatting.Indented)}");
                this.Tyres.OnSetupChange();
            } catch (IOException e) {
                RaceEngineerPlugin.LogInfo($"Setup changed. But cannot read new setup. Error: {e}");
                this.Setup = null;
            }
        }

        #endregion

    }

    public class FrontRear<T>(T f, T r) {
        public T F { get; internal set; } = f;
        public T R { get; internal set; } = r;

        public FrontRear(T one) : this(one, one) { }
    }

    internal class FrontRearPartial<T>(T f, T r) {
        internal T? F { get; set; } = f;
        internal T? R { get; set; } = r;

        internal FrontRear<T> Build() {
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

    internal class FrontRearJsonConverter<T> : JsonConverter<FrontRear<T>> where T : IEquatable<T> {
        public override void WriteJson(JsonWriter writer, FrontRear<T> value, JsonSerializer serializer) {
            writer.WriteStartObject();

            writer.WritePropertyName("F");
            serializer.Serialize(writer, value.F);

            if (!value.R.Equals(value.F)) {
                writer.WritePropertyName("R");
                serializer.Serialize(writer, value.R);
            }

            writer.WriteEndObject();
        }

        public override FrontRear<T> ReadJson(JsonReader reader, Type objectType, FrontRear<T> existingValue, bool hasExistingValue, JsonSerializer serializer) {
            throw new NotImplementedException("FrontRear should never be deserialized into. Use FrontRearPartial for it.");
        }
    }

    public class TyreInfo {
        public FrontRear<Lut> IdealPresCurve { get; }
        public FrontRear<Lut> IdealTempCurve { get; }
        public string? ShortName { get; }

        [JsonIgnore]
        public FrontRear<MinMaxAvg<double>> IdealPresRange { get; }
        [JsonIgnore]
        public FrontRear<MinMaxAvg<double>> IdealTempRange { get; }

        public TyreInfo(FrontRear<Lut> idealPresCurve, FrontRear<Lut> idealTempCurve, string? shortName = null) {
            this.IdealPresCurve = idealPresCurve;
            this.IdealTempCurve = idealTempCurve;
            this.ShortName = shortName;

            this.IdealPresRange = new(FindIdealMinMaxAvg(idealPresCurve.F), FindIdealMinMaxAvg(idealPresCurve.R));
            this.IdealTempRange = new(FindIdealMinMaxAvg(idealTempCurve.F), FindIdealMinMaxAvg(idealTempCurve.R));

        }

        private static MinMaxAvg<double> FindIdealMinMaxAvg(Lut lut) {
            var ienum = lut.Where(x => x.Item2 == 0.0).Select(x => x.Item1);
            var min = ienum.First();
            var max = ienum.Last();

            return new MinMaxAvg<double>(min, max, (min + max) / 2.0);
        }


        internal static TyreInfo Default() {
            return FromPartial(new TyreInfoPartial());
        }

        internal static TyreInfo FromPartial(TyreInfoPartial partial) {
            var idealPresCurve = partial.IdealPresCurve?.Build() ?? new(RaceEngineerPlugin.Settings.TyrePresNormalizationLut);
            var idealTempCurve = partial.IdealTempCurve?.Build() ?? new(RaceEngineerPlugin.Settings.TyreTempNormalizationLut);

            return new TyreInfo(idealPresCurve, idealTempCurve, partial.ShortName);
        }

        internal static TyreInfo FromACTyreInfo(ACTyreInfo acTyreInfo) {
            return new TyreInfo(
                PresCurvesFromACTyreInfo(acTyreInfo),
                TempCurvesFromACTyreInfo(acTyreInfo),
                acTyreInfo.ShortName
            );
        }

        internal static TyreInfo FromPartialAndACTyreInfo(TyreInfoPartial partial, ACTyreInfo acTyreInfo) {
            var idealPresCurve = partial.IdealPresCurve?.Build() ?? PresCurvesFromACTyreInfo(acTyreInfo);
            var idealTempCurve = partial.IdealTempCurve?.Build() ?? TempCurvesFromACTyreInfo(acTyreInfo);

            return new TyreInfo(idealPresCurve, idealTempCurve, partial.ShortName ?? acTyreInfo.ShortName);
        }

        private static FrontRear<Lut> PresCurvesFromACTyreInfo(ACTyreInfo acTyreInfo) {
            var idealPres = acTyreInfo.IdealPres;

            var presCurveF = new Lut([(idealPres.F - 1.0, -1.0), (idealPres.F - 0.25, 0.0), (idealPres.F + 0.25, 0.0), (idealPres.F + 1.0, 1.0)]);
            var presCurveR = new Lut([(idealPres.R - 1.0, -1.0), (idealPres.R - 0.25, 0.0), (idealPres.R + 0.25, 0.0), (idealPres.R + 1.0, 1.0)]);

            return new FrontRear<Lut>(presCurveF, presCurveR);
        }

        private static FrontRear<Lut> TempCurvesFromACTyreInfo(ACTyreInfo acTyreInfo) {
            static Lut NormalizeTempCurve(Lut curve) {
                var ys = curve.Y.ToList();

                // Normalize to [-1, 1]
                var overIdeal = false;
                for (int i = 0; i < curve.Length(); i++) {
                    // Make over ideal values be over 1.0
                    if (curve.Y[i] == 1.0) {
                        overIdeal = true;
                    } else if (overIdeal && curve.Y[i] < 1.0) {
                        ys[i] = 2.0 - curve.Y[i];
                    }

                    // Shift and scale values such that ideal is at 0.0, and AC's 0.95 is at +-1.0
                    ys[i] = Math.Round((curve.Y[i] - 1.0) * 20.0 * 100.0) / 100.0;
                }

                return new Lut(curve.X, ys.ToImmutableList());
            }

            return new FrontRear<Lut>(NormalizeTempCurve(acTyreInfo.TempCurveF), NormalizeTempCurve(acTyreInfo.TempCurveR));
        }
    }

    internal class TyreInfoPartial {
        internal FrontRearPartial<Lut>? IdealPresCurve { get; set; }
        internal FrontRearPartial<Lut>? IdealTempCurve { get; set; }
        internal string? ShortName { get; set; }

        internal void FillGaps(TyreInfoPartial other) {
            this.IdealPresCurve ??= other.IdealPresCurve;
            this.IdealTempCurve ??= other.IdealTempCurve;
            this.ShortName ??= other.ShortName;
        }

        internal bool IsFullyInitialized() {
            return this.IdealPresCurve != null && this.IdealTempCurve != null;
        }

        internal bool IsFullyInitializedAC() {
            return this.IdealPresCurve != null && this.IdealTempCurve != null && this.ShortName != null;
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
        public Dictionary<string, TyreInfo> Tyres { get; } = tyres;

        internal void Reset() {
            this.Tyres.Clear();
        }

        internal static CarInfo FromPartial(CarInfoPartial partial) {
            var result = new CarInfo([]);
            if (partial.Tyres == null) return result;

            foreach (var tyre in partial.Tyres!) {
                result.Tyres[tyre.Key] = TyreInfo.FromPartial(tyre.Value);
            }

            return result;
        }

        internal static CarInfo FromPartialAndACData(CarInfoPartial partial, ACCarInfo acCarInfo) {
            var result = new CarInfo([]);

            // Check for existing tyres and fill their gaps
            if (partial.Tyres != null) {
                foreach (var tyre in partial.Tyres!) {
                    if (acCarInfo.Tyres.ContainsKey(tyre.Key)) {
                        result.Tyres[tyre.Key] = TyreInfo.FromPartialAndACTyreInfo(tyre.Value, acCarInfo.Tyres[tyre.Key]);
                    } else {
                        result.Tyres[tyre.Key] = TyreInfo.FromPartial(tyre.Value);
                    }
                }
            }

            // Add tyres that were not present
            foreach (var tyre in acCarInfo.Tyres) {
                if (!result.Tyres.ContainsKey(tyre.Key)) {
                    result.Tyres[tyre.Key] = TyreInfo.FromACTyreInfo(tyre.Value);
                }
            }

            return result;
        }

        internal static CarInfo FromACCarInfo(ACCarInfo acCarInfo) {
            var result = new CarInfo([]);

            foreach (var tyre in acCarInfo.Tyres) {
                result.Tyres[tyre.Key] = TyreInfo.FromACTyreInfo(tyre.Value);
            }

            return result;
        }
    }

    internal class CarInfoPartial {
        internal Dictionary<string, TyreInfoPartial>? Tyres { get; set; }

        internal void FillGaps(CarInfoPartial other) {
            if (other.Tyres == null) return;

            if (this.Tyres == null) {
                this.Tyres = [];
            }

            // Add tyres that were not present
            foreach (var tyre in other.Tyres) {
                if (!this.Tyres.ContainsKey(tyre.Key)) {
                    this.Tyres[tyre.Key] = tyre.Value;
                } else {
                    this.Tyres[tyre.Key].FillGaps(tyre.Value);
                }
            }
        }

        internal bool IsFullyInitialized() {
            return this.Tyres != null && this.Tyres.All(a => a.Value.IsFullyInitialized());
        }

        internal bool IsFullyInitializedAC() {
            return this.Tyres != null && this.Tyres.All(a => a.Value.IsFullyInitializedAC());
        }
    }

    public class WheelsData<T> {
        private Func<T> _defGenerator { get; set; }
        private T[] _data { get; set; } = new T[4];

        internal WheelsData(Func<T> defGenerator) {
            this._defGenerator = defGenerator;
            for (int i = 0; i < 4; i++) {
                this._data[i] = this._defGenerator();
            }
        }

        internal WheelsData(T def) : this(() => def) { }

        internal void Reset() {
            for (int i = 0; i < 4; i++) {
                this._data[i] = this._defGenerator();
            }
        }

        public T FL { get => this._data[0]; internal set => this._data[0] = value; }
        public T FR { get => this._data[1]; internal set => this._data[1] = value; }
        public T RL { get => this._data[2]; internal set => this._data[2] = value; }
        public T RR { get => this._data[3]; internal set => this._data[3] = value; }

        public T this[int index] {
            get => this._data[index];
            internal set => this._data[index] = value;
        }

        internal void CopyTo(WheelsData<T> other, int index) {
            this._data.CopyTo(other._data, index);
        }
    }

    public class ImmutableWheelsData<T> {
        private T[] _data { get; set; } = new T[4];

        internal ImmutableWheelsData(T fl, T fr, T rl, T rr) {
            this._data = [fl, fr, rl, rr];
        }

        public T FL => this._data[0];
        public T FR => this._data[1];
        public T RL => this._data[2];
        public T RR => this._data[3];

        public T this[int index] => this._data[index];
    }

    internal class ACTyreInfo(string name, string shortName, Lut wearCurveF, Lut wearCurveR, Lut tempCurveF, Lut tempCurveR, FrontRear<double> idealPres) {
        internal string Name { get; private set; } = name;
        internal string ShortName { get; private set; } = shortName;
        internal Lut WearCurveF { get; private set; } = wearCurveF;
        internal Lut WearCurveR { get; private set; } = wearCurveR;

        internal FrontRear<double> IdealPres { get; private set; } = idealPres;

        internal Lut TempCurveF { get; private set; } = tempCurveF;
        internal Lut TempCurveR { get; private set; } = tempCurveR;
    }

    internal class ACCarInfo {
        enum FrontOrRear { F, R }
        internal class ACTyreInfoPartial {
            internal string? Name { get; set; }
            internal string? ShortName { get; set; }
            internal Lut? WearCurveF { get; set; }
            internal Lut? WearCurveR { get; set; }

            internal double? IdealPresF { get; set; }
            internal double? IdealPresR { get; set; }

            internal Lut? TempCurveF { get; set; }
            internal Lut? TempCurveR { get; set; }

            internal ACTyreInfo Build() {
                return new ACTyreInfo(this.Name!, this.ShortName!, this.WearCurveF!, this.WearCurveR!, this.TempCurveF!, this.TempCurveR!, new FrontRear<double>((double)this.IdealPresF!, (double)this.IdealPresR!));
            }
        }

        internal Dictionary<string, ACTyreInfo> Tyres { get; private set; } = [];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"> Path to a folder containing the tyres.ini file and other LUTs.</param>
        /// <returns></returns>
        internal static ACCarInfo FromFile(string path) {
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
                    info.Tyres[$"{r.Name} ({r.ShortName})"] = r.Build();
                }
            }

            return info;
        }
    }

    public class Lut : IEnumerable<(double, double)>, IEquatable<Lut> {
        public ImmutableList<double> X { get; }
        public ImmutableList<double> Y { get; }

        internal Lut() {
            this.X = ImmutableList<double>.Empty;
            this.Y = ImmutableList<double>.Empty;
        }

        internal Lut((double, double)[] values) : this() {
            var xs = new List<double>(values.Length);
            var ys = new List<double>(values.Length);

            for (int i = 0; i < values.Length; i++) {
                xs.Add(values[i].Item1);
                ys.Add(values[i].Item2);
            }

            this.X = xs.ToImmutableList();
            this.Y = ys.ToImmutableList();
        }

        internal Lut(ImmutableList<double> xs, ImmutableList<double> ys) {
            if (xs.Count != ys.Count) {
                throw new Exception("There must be same number of x and y values.");
            }
            this.X = xs;
            this.Y = ys;
        }

        internal static Lut FromFileAC(string path) {
            return FromFile(path);
        }

        internal static Lut FromFile(string path) {
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

        public int Length() {
            return this.X.Count;
        }

        public IEnumerator<(double, double)> GetEnumerator() {
            for (int i = 0; i < this.Length(); i++) {
                yield return (this.X[i], this.Y[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        public bool Equals(Lut other) {
            return this.X.SequenceEqual(other.X) && this.Y.SequenceEqual(other.Y);
        }
    }

    internal class LutJsonConverter : JsonConverter<Lut> {
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
            var xs = new List<double>();
            var ys = new List<double>();

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

                    xs.Add((double)x!);
                    ys.Add((double)y!);

                    reader.Read(); // eat array end
                    if (reader.TokenType != JsonToken.EndArray) {
                        throw new Exception("Invalid JSON");
                    }
                    continue;
                }

                if (reader.TokenType == JsonToken.EndArray) break;
            }

            return new Lut(xs.ToImmutableList(), ys.ToImmutableList());


        }
    }
}