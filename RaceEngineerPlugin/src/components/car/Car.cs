using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using GameReaderCommon;

using KLPlugins.RaceEngineer.Stats;

using MathNet.Numerics.LinearAlgebra.Factorization;

using Newtonsoft.Json;

[assembly: InternalsVisibleToAttribute("RaceEngineerPluginTests")]

namespace KLPlugins.RaceEngineer.Car {
    /// <summary>
    /// Store and update car related values
    /// </summary>
    public class Car {
        public string? Name { get; private set; } = null;
        public CarInfo Info { get; private set; } = new([]);
        public CarSetup? Setup { get; private set; } = null;

        // NOTE: It's important to never reassign these values. 
        // The property exports to SimHub rely on the fact that they point to one place always.
        public Tyres Tyres { get; } = new();
        public Brakes Brakes { get; } = new();
        public Fuel Fuel { get; } = new();

        internal Car() {
            RaceEngineerPlugin.LogInfo("Created new Car");
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
                var txt = File.ReadAllText(fname).Replace("\"", "'");
                var partial = JsonConvert.DeserializeObject<CarInfo.Partial>(txt);
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

            CarInfo.Partial partialInfo = new(null);

            // 1. Try to read car specific data file
            try {
                var txt = File.ReadAllText(pluginsCarDataPath).Replace("\"", "'");
                var partial = JsonConvert.DeserializeObject<CarInfo.Partial>(txt);

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
                var acinfo = ACCarInfo.FromFiles(ACRawDataPath);
                // if we didn't throw then acinfo does contain all required data
                this.Info = CarInfo.FromPartialAndACData(partialInfo, acinfo);

                // Write the data out, so that we don't need to go through it next time
                var json = JsonConvert.SerializeObject(this.Info, Formatting.Indented);
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
                var partial = JsonConvert.DeserializeObject<CarInfo.Partial>(txt);

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

    internal class FrontRearJsonConverter<T> : JsonConverter<FrontRear<T>>
        where T : IEquatable<T> {
        private readonly JsonConverter<T>? _tConverter = null;

        public FrontRearJsonConverter() { }

        public FrontRearJsonConverter(JsonConverter<T> tConverter) {
            _tConverter = tConverter;
        }

        public override void WriteJson(JsonWriter writer, FrontRear<T>? value, JsonSerializer serializer) {
            if (value == null) {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName("F");
            this.WriteT(writer, value.F, serializer);

            if (!value.R.Equals(value.F)) {
                writer.WritePropertyName("R");
                this.WriteT(writer, value.R, serializer);
            }

            writer.WriteEndObject();
        }

        void WriteT(JsonWriter writer, T value, JsonSerializer serializer) {
            if (_tConverter != null) {
                _tConverter.WriteJson(writer, value, serializer);
            } else {
                serializer.Serialize(writer, value);
            }
        }

        public override FrontRear<T> ReadJson(JsonReader reader, Type objectType, FrontRear<T>? existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.StartObject) {
                throw new Exception($"Invalid JSON. Expected '{JsonToken.StartObject}'. Found '{reader.TokenType}: {reader.Value}'.");
            }

            T? f = default;
            T? r = default;

            reader.Read();

            if (reader.TokenType != JsonToken.PropertyName || reader.Value == null) {
                throw new Exception($"Invalid JSON. Expected '{JsonToken.PropertyName}'. Found '{reader.TokenType}: {reader.Value}'.");
            }

            if ((string)reader.Value! == "F") {
                reader.Read();
                f = this.ReadT(reader, objectType, existingValue == null ? default : existingValue.F, hasExistingValue, serializer);
            } else if ((string)reader.Value! == "R") {
                reader.Read();
                r = this.ReadT(reader, objectType, existingValue == null ? default : existingValue.R, hasExistingValue, serializer);
            } else {
                throw new Exception($"Invalid JSON. Expected '{JsonToken.PropertyName}' with value of 'F' or 'R'. Found '{reader.TokenType}: {reader.Value}'.");
            }

            reader.Read();
            if (reader.TokenType == JsonToken.PropertyName) {
                if (reader.Value == null) {
                    throw new Exception($"Invalid JSON. Expected '{JsonToken.PropertyName}'. Found '{reader.TokenType}: {reader.Value}'.");
                }

                if ((string)reader.Value! == "R") {
                    if (r == null) {
                        reader.Read();
                        r = this.ReadT(reader, objectType, existingValue == null ? default : existingValue.R, hasExistingValue, serializer);
                    } else {
                        throw new Exception($"Invalid JSON. Found double property 'R'. Found '{reader.TokenType}: {reader.Value}'.");
                    }
                } else if ((string)reader.Value! == "F") {
                    if (f == null) {
                        reader.Read();
                        f = this.ReadT(reader, objectType, existingValue == null ? default : existingValue.F, hasExistingValue, serializer);
                    } else {
                        throw new Exception($"Invalid JSON. Found double property ''. Found '{reader.TokenType}: {reader.Value}'.");
                    }
                }

                reader.Read();
            }

            if (reader.TokenType != JsonToken.EndObject) {
                throw new Exception($"Invalid JSON. Expected '{JsonToken.EndObject}'. Found '{reader.TokenType}: {reader.Value}'.");
            }

            if (f == null && r == null) {
                throw new Exception("Invalid JSON. Both R and F are missing.");
            } else if (r != null && f == null) {
                return new(r);
            } else if (f != null && r == null) {
                return new(f);
            } else if (f != null && r != null) {
                return new(f, r);
            } else {
                throw new Exception("unreachable branch");
            }
        }

        T? ReadT(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (_tConverter != null) {
                return _tConverter.ReadJson(reader, objectType, existingValue, hasExistingValue, serializer);
            } else {
                return serializer.Deserialize<T>(reader);
            }
        }
    }

    public class TyreInfo {
        [JsonConverter(typeof(FrontRearJsonConverter<Lut>))]
        [JsonProperty(Required = Required.Always)]
        public FrontRear<Lut> IdealPresCurve { get; }

        [JsonConverter(typeof(FrontRearJsonConverter<Lut>))]
        [JsonProperty(Required = Required.Always)]
        public FrontRear<Lut> IdealTempCurve { get; }

        [JsonProperty(Required = Required.Always)]
        public string? ShortName { get; }

        [JsonIgnore]
        public FrontRear<ImmutableMinMaxAvg<double>> IdealPresRange { get; }

        [JsonIgnore]
        public FrontRear<ImmutableMinMaxAvg<double>> IdealTempRange { get; }

        public TyreInfo(FrontRear<Lut> idealPresCurve, FrontRear<Lut> idealTempCurve, string? shortName = null) {
            this.IdealPresCurve = idealPresCurve;
            this.IdealTempCurve = idealTempCurve;
            this.ShortName = shortName;

            this.IdealPresRange = new(FindIdealMinMaxAvg(idealPresCurve.F), FindIdealMinMaxAvg(idealPresCurve.R));
            this.IdealTempRange = new(FindIdealMinMaxAvg(idealTempCurve.F), FindIdealMinMaxAvg(idealTempCurve.R));
        }

        private static ImmutableMinMaxAvg<double> FindIdealMinMaxAvg(Lut lut) {
            var ienum = lut.Where(x => x.Item2 == 0.0).Select(x => x.Item1);
            var min = ienum.First();
            var max = ienum.Last();

            return new ImmutableMinMaxAvg<double>(min, max, (min + max) / 2.0);
        }

        internal static TyreInfo Default() {
            return FromPartial(new Partial());
        }

        internal static TyreInfo FromPartial(Partial partial) {
            var idealPresCurve = partial.IdealPresCurve ?? new(RaceEngineerPlugin.Settings.TyrePresNormalizationLut);
            var idealTempCurve = partial.IdealTempCurve ?? new(RaceEngineerPlugin.Settings.TyreTempNormalizationLut);

            return new TyreInfo(idealPresCurve, idealTempCurve, partial.ShortName);
        }

        internal static TyreInfo FromACTyreInfo(ACTyreInfo acTyreInfo) {
            return new TyreInfo(
                PresCurvesFromACTyreInfo(acTyreInfo),
                TempCurvesFromACTyreInfo(acTyreInfo),
                acTyreInfo.ShortName
            );
        }

        internal static TyreInfo FromPartialAndACTyreInfo(Partial partial, ACTyreInfo acTyreInfo) {
            var idealPresCurve = partial.IdealPresCurve ?? PresCurvesFromACTyreInfo(acTyreInfo);
            var idealTempCurve = partial.IdealTempCurve ?? TempCurvesFromACTyreInfo(acTyreInfo);

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

        internal class Partial {

            [JsonProperty]
            [JsonConverter(typeof(FrontRearJsonConverter<Lut>))]
            internal FrontRear<Lut>? IdealPresCurve { get; set; }

            [JsonProperty]
            [JsonConverter(typeof(FrontRearJsonConverter<Lut>))]
            internal FrontRear<Lut>? IdealTempCurve { get; set; }

            [JsonProperty]
            internal string? ShortName { get; set; }

            internal Partial() { }

            [JsonConstructor]
            internal Partial(FrontRear<Lut>? idealPresCurve, FrontRear<Lut>? idealTempCurve, string? shortName) {
                this.IdealPresCurve = idealPresCurve;
                this.IdealTempCurve = idealTempCurve;
                this.ShortName = shortName;
            }

            internal void FillGaps(Partial other) {
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
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, TyreInfo> Tyres { get; } = tyres;

        internal void Reset() {
            this.Tyres.Clear();
        }

        internal static CarInfo FromPartial(Partial partial) {
            var result = new CarInfo([]);
            if (partial.Tyres == null) return result;

            foreach (var tyre in partial.Tyres!) {
                result.Tyres[tyre.Key] = TyreInfo.FromPartial(tyre.Value);
            }

            return result;
        }

        internal static CarInfo FromPartialAndACData(Partial partial, ACCarInfo acCarInfo) {
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

        internal class Partial {
            [JsonProperty]
            internal Dictionary<string, TyreInfo.Partial>? Tyres { get; set; }

            [JsonConstructor]
            internal Partial(Dictionary<string, TyreInfo.Partial>? tyres) {
                this.Tyres = tyres;
            }

            internal void FillGaps(Partial other) {
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
                return this.Tyres != null && this.Tyres.Count != 0 && this.Tyres.All(a => a.Value.IsFullyInitialized());
            }

            internal bool IsFullyInitializedAC() {
                return this.Tyres != null && this.Tyres.Count != 0 && this.Tyres.All(a => a.Value.IsFullyInitializedAC());
            }
        }
    }

    internal interface IWheelsData<T> {
        public T FL { get; }
        public T FR { get; }
        public T RL { get; }
        public T RR { get; }

        public T this[int index] { get; }
    }

    internal class WheelsData<T> : IWheelsData<T> {
        private Func<T> _defGenerator { get; set; }

        internal WheelsData(Func<T> defGenerator) {
            this._defGenerator = defGenerator;
            this.FL = this._defGenerator();
            this.FR = this._defGenerator();
            this.RL = this._defGenerator();
            this.RR = this._defGenerator();
        }

        internal WheelsData(T def) : this(() => def) { }

        internal void Reset() {
            this.FL = this._defGenerator();
            this.FR = this._defGenerator();
            this.RL = this._defGenerator();
            this.RR = this._defGenerator();
        }

        public T FL { get; internal set; }
        public T FR { get; internal set; }
        public T RL { get; internal set; }
        public T RR { get; internal set; }

        public T this[int index] {
            get {
                return index switch {
                    0 => this.FL,
                    1 => this.FR,
                    2 => this.RL,
                    3 => this.RR,
                    _ => throw new IndexOutOfRangeException()
                };
            }
            internal set {
                switch (index) {
                    case 0: this.FL = value; break;
                    case 1: this.FR = value; break;
                    case 2: this.RL = value; break;
                    case 3: this.RR = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        internal void CopyTo(WheelsData<T> other) {
            other.FL = this.FL;
            other.FR = this.FR;
            other.RL = this.RL;
            other.RR = this.RR;
        }

        /// <summary>
        /// Creates a readonly view of the wheels data.
        /// 
        /// Note that this container and the returned container share the underlying data storage.
        /// Thus while the returned container is itself readonly, the parent (this class) is not 
        /// and anyone having access to it can still modify it. Thus the data in the returned container
        /// may change between two accesses.
        /// </summary>
        /// <returns></returns>
        public ReadonlyWheelsDataView<T> AsReadonlyView() {
            return new ReadonlyWheelsDataView<T>(this);
        }

        public ImmutableWheelsData<T> ToImmutableWheelsDataShallow() {
            return new(this.FL, this.FR, this.RL, this.RR);
        }
    }

    public readonly struct ReadonlyWheelsDataView<T> : IWheelsData<T> {
        private readonly WheelsData<T> _data;

        internal ReadonlyWheelsDataView(WheelsData<T> values) {
            this._data = values;
        }

        public T FL => this._data.FL;
        public T FR => this._data.FR;
        public T RL => this._data.RL;
        public T RR => this._data.RR;

        public T this[int index] => this._data[index];

        public ImmutableWheelsData<T> ToImmutableWheelsDataShallow() {
            return this._data.ToImmutableWheelsDataShallow();
        }
    }

    public class ImmutableWheelsData<T>(T fl, T fr, T rl, T rr) : IWheelsData<T> {
        public T FL => fl;
        public T FR => fr;
        public T RL => rl;
        public T RR => rr;

        public T this[int index] {
            get {
                return index switch {
                    0 => this.FL,
                    1 => this.FR,
                    2 => this.RL,
                    3 => this.RR,
                    _ => throw new IndexOutOfRangeException()
                };
            }
        }
    }

    internal class ImmutableWheelsDataAsArrayJsonConverter<T> : JsonConverter<ImmutableWheelsData<T>> {
        public override void WriteJson(JsonWriter writer, ImmutableWheelsData<T>? value, JsonSerializer serializer) {
            if (value == null) {
                writer.WriteNull();
                return;
            }

            writer.WriteStartArray();
            writer.WriteValue(value.FL);
            writer.WriteValue(value.FR);
            writer.WriteValue(value.RL);
            writer.WriteValue(value.RR);
            writer.WriteEndArray();
        }

        public override ImmutableWheelsData<T> ReadJson(JsonReader reader, Type objectType, ImmutableWheelsData<T>? existingValue, bool hasExistingValue, JsonSerializer serializer) {
            var xs = serializer.Deserialize<T[]>(reader) ?? throw new Exception("Invalid JSON");
            if (xs.Length != 4) {
                throw new Exception("Invalid JSON");
            }

            return new(xs[0], xs[1], xs[2], xs[3]);
        }
    }

    internal class ACTyreInfo(
        string name,
        string shortName,
        Lut wearCurveF,
        Lut wearCurveR,
        Lut tempCurveF,
        Lut tempCurveR,
        FrontRear<double> idealPres
    ) {

        [JsonProperty]
        internal string Name { get; private set; } = name;

        [JsonProperty]
        internal string ShortName { get; private set; } = shortName;

        [JsonProperty]
        internal Lut WearCurveF { get; private set; } = wearCurveF;

        [JsonProperty]
        internal Lut WearCurveR { get; private set; } = wearCurveR;

        [JsonProperty]
        internal FrontRear<double> IdealPres { get; private set; } = idealPres;

        [JsonProperty]
        internal Lut TempCurveF { get; private set; } = tempCurveF;

        [JsonProperty]
        internal Lut TempCurveR { get; private set; } = tempCurveR;
    }

    internal class ACCarInfo {
        [JsonProperty]
        internal Dictionary<string, ACTyreInfo> Tyres { get; private set; } = [];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderPath"> Path to a folder containing the tyres.ini file and other LUTs.</param>
        /// <returns></returns>
        internal static ACCarInfo FromFiles(string folderPath) {
            var info = new ACCarInfo();
            Dictionary<int, ACTyreInfoPartial> results = [];

            var folder_path = folderPath + "\\";
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

        private enum FrontOrRear { F, R }
        private class ACTyreInfoPartial {
            internal string? Name { get; set; }
            internal string? ShortName { get; set; }
            internal Lut? WearCurveF { get; set; }
            internal Lut? WearCurveR { get; set; }
            internal double? IdealPresF { get; set; }
            internal double? IdealPresR { get; set; }
            internal Lut? TempCurveF { get; set; }
            internal Lut? TempCurveR { get; set; }

            internal ACTyreInfo Build() {
                return new ACTyreInfo(
                    this.Name!,
                    this.ShortName!,
                    this.WearCurveF!,
                    this.WearCurveR!,
                    this.TempCurveF!,
                    this.TempCurveR!,
                    new FrontRear<double>((double)this.IdealPresF!, (double)this.IdealPresR!)
                );
            }
        }
    }

    [JsonConverter(typeof(Lut.JsonConverter))]
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
            var xs = new List<double>();
            var ys = new List<double>();

            var txt = File.ReadAllText(path);

            // each line is: from|to
            // optionally there are comment that start with ;
            foreach (var l in txt.Split('\n')) {
                var line = l.Trim();
                if (line == "" || line.StartsWith(";")) continue;

                var parts = line.Split('|');
                var x = Convert.ToDouble(parts[0].Trim());
                var y = Convert.ToDouble(parts[1].Split(';')[0].Trim());

                xs.Add(x);
                ys.Add(y);
            }

            return new Lut(xs.ToImmutableList(), ys.ToImmutableList());
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

        internal class JsonConverter : JsonConverter<Lut> {
            public override void WriteJson(JsonWriter writer, Lut? value, JsonSerializer serializer) {
                if (value == null) {
                    writer.WriteNull();
                    return;
                }

                writer.WriteStartArray();

                for (int i = 0; i < value.X.Count; i++) {
                    writer.WriteStartArray();
                    writer.WriteValue(value.X[i]);
                    writer.WriteValue(value.Y[i]);
                    writer.WriteEndArray();
                }

                writer.WriteEndArray();
            }

            public override Lut ReadJson(JsonReader reader, Type objectType, Lut? existingValue, bool hasExistingValue, JsonSerializer serializer) {
                var xs = new List<double>();
                var ys = new List<double>();

                if (reader.TokenType != JsonToken.StartArray) {
                    throw new Exception($"Invalid JSON. Expected '{JsonToken.StartArray}'. Found '{reader.TokenType}: {reader.Value}'.");
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
                            throw new Exception($"Invalid JSON. Expected '{JsonToken.EndArray}'. Found '{reader.TokenType}: {reader.Value}'.");
                        }
                        continue;
                    }

                    if (reader.TokenType == JsonToken.EndArray) break;
                }

                return new Lut(xs.ToImmutableList(), ys.ToImmutableList());
            }
        }
    }

}