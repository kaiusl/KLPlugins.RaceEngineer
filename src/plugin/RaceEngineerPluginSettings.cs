using System;
using System.IO;
using System.Linq;
using System.Security.Policy;

using KLPlugins.RaceEngineer.Car;

using Newtonsoft.Json;

namespace KLPlugins.RaceEngineer {
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class RaceEngineerPluginSettings {
        public int SpeedWarningLevel = 100;
    }

    public class Settings {
        public int NumPreviousValuesStored { get; set; }
        public string DataLocation { get; set; }
        public string AccDataLocation { get; set; }

        public Lut BrakeTempNormalizationLut { get; set; }
        public Lut TyreTempNormalizationLut { get; set; }
        public Lut TyrePresNormalizationLut { get; set; }
        public double IdealPres { get; set; }

        public bool Log { get; set; }
        public bool ShowAllLaps { get; set; }

        public StatsFlags PrevLapsStatsFlags => this._prevLapsStats;
        public StatsFlags PrevFuelPerLapStatsFlags => this._prevFuelPerLapStats;
        public StatsFlags RemainingStatsFlags => this._remainingStats;
        public WheelFlags TyrePresFlags => this._tyrePresFlags;
        public WheelFlags TyreTempFlags => this._tyreTempFlags;
        public WheelFlags BrakeTempFlags => this._brakeTempFlags;

        private readonly StatsFlags _prevLapsStats;
        private readonly StatsFlags _prevFuelPerLapStats;
        private readonly StatsFlags _remainingStats;
        private readonly WheelFlags _tyrePresFlags;
        private readonly WheelFlags _tyreTempFlags;
        private readonly WheelFlags _brakeTempFlags;

        private const string SETTINGS_PATH = @"PluginsData\KLPlugins\RaceEngineer\Settings.json";
        private static readonly string[] REMAINING_STATS_FLAGS = ["Min", "Max", "Avg"];

        public Settings() {
            SettingsInternal s = new SettingsInternal();
            if (File.Exists(SETTINGS_PATH)) {
                var text = File.ReadAllText(SETTINGS_PATH).Replace("\"", "'");
                var sTmp = JsonConvert.DeserializeObject<SettingsInternal>(text, new LutJsonConverter());
                if (sTmp != null) {
                    s = sTmp;
                } else {
                    SimHub.Logging.Current.Error("Error deserializing settings file: " + SETTINGS_PATH);
                }
            } else {
                string txt = JsonConvert.SerializeObject(s, Formatting.Indented, new LutJsonConverter());
                Directory.CreateDirectory(Path.GetDirectoryName(SETTINGS_PATH));
                File.WriteAllText(SETTINGS_PATH, txt);
            }

            this.NumPreviousValuesStored = s.NumPreviousValuesStored;
            this.DataLocation = s.DataLocation;
            this.AccDataLocation = s.AccDataLocation;
            this.Log = s.Log;
            this.ShowAllLaps = s.ShowAllLaps;

            this.BrakeTempNormalizationLut = s.BrakeTempNormalizationLut;
            this.TyreTempNormalizationLut = s.TyreTempNormalizationLut;
            this.TyrePresNormalizationLut = s.TyrePresNormalizationLut;

            this.IdealPres = s.IdealPres;

            this.ParseLapFlags(s.PrevLapsInfo, ref this._prevLapsStats, "PrevLapsInfo");
            this.ParseStatsFlags(s.PrevFuelPerLapInfo, ref this._prevFuelPerLapStats, "PrevFuelPerLapInfo");
            this.ParseRemainingStatsFlags(s.RemainingInfo, ref this._remainingStats, "RemainingInfo");
            this.ParseWheelFlags(s.TyrePresInfo, ref this._tyrePresFlags, "TyrePresInfo");
            this.ParseWheelFlags(s.TyreTempInfo, ref this._tyreTempFlags, "TyreTempInfo");
            this.ParseWheelFlags(s.BrakeTempInfo, ref this._brakeTempFlags, "BrakeTempInfo");

        }

        public void ParseLapFlags(string[] stats, ref StatsFlags statsFlags, string varName) {
            foreach (var v in stats) {
                if (Enum.TryParse(v, out StatsFlags newVariant)) {
                    statsFlags |= newVariant;
                } else {
                    RaceEngineerPlugin.LogWarn($"Found unknown setting '{v}' in {varName}");
                }
            }
        }

        public void ParseStatsFlags(string[] stats, ref StatsFlags flags, string varName) {
            foreach (var v in stats) {
                if (Enum.TryParse(v, out StatsFlags newVariant)) {
                    flags |= newVariant;
                } else {
                    RaceEngineerPlugin.LogWarn($"Found unknown setting '{v}' in {varName}");
                }
            }
        }

        public void ParseRemainingStatsFlags(string[] stats, ref StatsFlags flags, string varName) {
            foreach (var v in stats) {
                if (REMAINING_STATS_FLAGS.Contains(v) && Enum.TryParse(v, out StatsFlags newVariant)) {
                    flags |= newVariant;
                } else {
                    RaceEngineerPlugin.LogWarn($"Found unknown setting '{v}' in {varName}");
                }
            }
        }

        public void ParseWheelFlags(string[] stats, ref WheelFlags flags, string varName) {
            foreach (var v in stats) {
                if (Enum.TryParse(v, out WheelFlags newVariant)) {
                    flags |= newVariant;
                } else {
                    RaceEngineerPlugin.LogWarn($"Found unknown setting '{v}' in {varName}");
                }
            }
        }
    }


    [Flags]
    public enum StatsFlags {
        None = 0,
        Min = 1 << 0,
        Max = 1 << 1,
        Avg = 1 << 2,
        Std = 1 << 3,
        Median = 1 << 4,
        Q1 = 1 << 5,
        Q3 = 1 << 6
    }


    [Flags]
    public enum WheelFlags {
        None = 0,
        Min = 1 << 0,
        Max = 1 << 1,
        Avg = 1 << 2,
        Std = 1 << 3,
        MinColor = 1 << 4,
        MaxColor = 1 << 5,
        AvgColor = 1 << 6,
        Color = 1 << 7,
    }



    public class SettingsInternal {
        public int NumPreviousValuesStored { get; set; }
        public string DataLocation { get; set; }
        public string AccDataLocation { get; set; }


        public Lut BrakeTempNormalizationLut { get; set; }
        public Lut TyreTempNormalizationLut { get; set; }
        public Lut TyrePresNormalizationLut { get; set; }
        public double IdealPres { get; set; }

        public bool Log { get; set; }
        public bool ShowAllLaps { get; set; }

        public string[] PrevLapsInfo { get; set; }
        public string[] PrevFuelPerLapInfo { get; set; }

        public string[] TyrePresInfo { get; set; }
        public string[] TyreTempInfo { get; set; }
        public string[] BrakeTempInfo { get; set; }

        public string[] RemainingInfo { get; set; }


        public SettingsInternal() {
            this.NumPreviousValuesStored = 10;
            this.DataLocation = "PluginsData\\KLPlugins\\RaceEngineer";
            this.AccDataLocation = "C:\\Users\\" + Environment.UserName + "\\Documents\\Assetto Corsa Competizione";
            this.BrakeTempNormalizationLut = new Lut([(200, -1.0), (300, 0.0), (500, 0.0), (700, 1.0)]);
            this.TyreTempNormalizationLut = new Lut([(70.0, -1.0), (80.0, 0.0), (90.0, 0.0), (100.0, 1.0)]);
            this.TyrePresNormalizationLut = new Lut([(26.5, -1.0), (27.25, 0.0), (27.75, 0.0), (28.5, 1.0)]);
            this.IdealPres = 27.5;

            //  this.TempColor = ["#87cefa", "#00ff7f", "#00ff7f", "#e60000"];
            // this.TyreTempColorDefValues = [70.0, 80.0, 90.0, 100.0];
            // this.BrakeTempColorDefValues = [200.0, 300.0, 500.0, 700.0];
            // this.PresColor = ["#87cefa", "#00ff7f", "#00ff7f", "#e60000"];
            // this.TyrePresColorDefValues = [26.5, 27.25, 27.75, 28.5];
            // this.TimeColor = ["#00ff7f", "#F8F8FF", "#e60000"];
            // this.TimeGraphColor = ["#00ff7f", "#F8F8FF", "#e60000"];
            // this.TimeColorDeltaValues = [-1.0, 0.0, 1.0];
            // this.FuelGraphColor = ["#00ff7f", "#F8F8FF", "#e60000"];
            // this.DefColor = "#555555";
            // this.FuelGraphColorValues = [-1.0, 0.0, 1.0];
            this.Log = true;
            this.ShowAllLaps = false;
            this.PrevLapsInfo = ["Min", "Max", "Avg", "Std", "Q1", "Median", "Q3"];
            this.PrevFuelPerLapInfo = ["Min", "Max", "Avg", "Std", "Q1", "Median", "Q3"];
            this.TyrePresInfo = ["Min", "Max", "Avg", "Std", "MinColor", "MaxColor", "AvgColor", "Color"];
            this.TyreTempInfo = ["Min", "Max", "Avg", "Std", "MinColor", "MaxColor", "AvgColor", "Color"];
            this.BrakeTempInfo = ["Min", "Max", "Avg", "Std", "MinColor", "MaxColor", "AvgColor", "Color"];
            this.RemainingInfo = ["Min", "Max", "Avg"];
        }
    }
}