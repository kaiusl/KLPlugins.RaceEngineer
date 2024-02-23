﻿using System;
using System.IO;
using System.Linq;

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

        public string[] TempColor { get; set; }
        public double[] TyreTempColorDefValues { get; set; }
        public double[] BrakeTempColorDefValues { get; set; }
        public string[] PresColor { get; set; }
        public double[] TyrePresColorDefValues { get; set; }
        public string[] TimeColor { get; set; }
        public string[] TimeGraphColor { get; set; }

        public double[] TimeColorDeltaValues { get; set; }
        public string[] FuelGraphColor { get; set; }
        public double[] FuelGraphColorValues { get; set; }
        public bool Log { get; set; }
        public bool ShowAllLaps { get; set; }

        public StatsFlags PrevLapsStatsFlags => this._prevLapsStats;
        public StatsFlags PrevFuelPerLapStatsFlags => this._prevFuelPerLapStats;
        public StatsFlags RemainingStatsFlags => this._remainingStats;
        public WheelFlags TyrePresFlags => this._tyrePresFlags;
        public WheelFlags TyreTempFlags => this._tyreTempFlags;
        public WheelFlags BrakeTempFlags => this._brakeTempFlags;

        private StatsFlags _prevLapsStats;
        private StatsFlags _prevFuelPerLapStats;
        private StatsFlags _remainingStats;
        private WheelFlags _tyrePresFlags;
        private WheelFlags _tyreTempFlags;
        private WheelFlags _brakeTempFlags;

        private const string SETTINGS_PATH = @"PluginsData\KLPlugins\RaceEngineer\Settings.json";
        private static readonly string[] REMAINING_STATS_FLAGS = { "Min", "Max", "Avg" };

        public Settings() {
            SettingsInternal s;
            if (File.Exists(SETTINGS_PATH)) {
                s = JsonConvert.DeserializeObject<SettingsInternal>(File.ReadAllText(SETTINGS_PATH).Replace("\"", "'"));
            } else {
                s = new SettingsInternal();
                string txt = JsonConvert.SerializeObject(s, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(SETTINGS_PATH));
                File.WriteAllText(SETTINGS_PATH, txt);
            }

            this.NumPreviousValuesStored = s.NumPreviousValuesStored;
            this.DataLocation = s.DataLocation;
            this.AccDataLocation = s.AccDataLocation;
            this.TempColor = s.TempColor;
            this.TyreTempColorDefValues = s.TyrePresColorDefValues;
            this.BrakeTempColorDefValues = s.BrakeTempColorDefValues;
            this.PresColor = s.PresColor;
            this.TyrePresColorDefValues = s.TyrePresColorDefValues;
            this.TimeColor = s.TimeColor;
            this.TimeGraphColor = s.TimeGraphColor;
            this.TimeColorDeltaValues = s.TimeColorDeltaValues;
            this.FuelGraphColor = s.FuelGraphColor;
            this.FuelGraphColorValues = s.FuelGraphColorValues;
            this.Log = s.Log;
            this.ShowAllLaps = s.ShowAllLaps;


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

        public string[] TempColor { get; set; }
        public double[] TyreTempColorDefValues { get; set; }
        public double[] BrakeTempColorDefValues { get; set; }
        public string[] PresColor { get; set; }
        public double[] TyrePresColorDefValues { get; set; }
        public string[] TimeColor { get; set; }
        public string[] TimeGraphColor { get; set; }

        public double[] TimeColorDeltaValues { get; set; }
        public string[] FuelGraphColor { get; set; }
        public double[] FuelGraphColorValues { get; set; }
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
            this.TempColor = new string[] { "#87cefa", "#00ff7f", "#00ff7f", "#e60000" };
            this.TyreTempColorDefValues = new double[] { 70.0, 80.0, 90.0, 100.0 };
            this.BrakeTempColorDefValues = new double[] { 200.0, 300.0, 500.0, 700.0 };
            this.PresColor = new string[] { "#87cefa", "#00ff7f", "#00ff7f", "#e60000" };
            this.TyrePresColorDefValues = new double[] { 26.5, 27.25, 27.75, 28.5 };
            this.TimeColor = new string[] { "#00ff7f", "#F8F8FF", "#e60000" };
            this.TimeGraphColor = new string[] { "#00ff7f", "#F8F8FF", "#e60000" };
            this.TimeColorDeltaValues = new double[] { -1.0, 0.0, 1.0 };
            this.FuelGraphColor = new string[] { "#00ff7f", "#F8F8FF", "#e60000" };
            this.FuelGraphColorValues = new double[] { -1.0, 0.0, 1.0 };
            this.Log = true;
            this.ShowAllLaps = false;
            this.PrevLapsInfo = new string[] { "Min", "Max", "Avg", "Std", "Q1", "Median", "Q3" };
            this.PrevFuelPerLapInfo = new string[] { "Min", "Max", "Avg", "Std", "Q1", "Median", "Q3" };
            this.TyrePresInfo = new string[] { "Min", "Max", "Avg", "Std", "MinColor", "MaxColor", "AvgColor", "Color" };
            this.TyreTempInfo = new string[] { "Min", "Max", "Avg", "Std", "MinColor", "MaxColor", "AvgColor", "Color" };
            this.BrakeTempInfo = new string[] { "Min", "Max", "Avg", "Std", "MinColor", "MaxColor", "AvgColor", "Color" };
            this.RemainingInfo = new string[] { "Min", "Max", "Avg" };
        }
    }
}