using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace RaceEngineerPlugin {
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class RaceEngineerPluginSettings
    {
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

        public StatsFlags PrevLapsStatsFlags { get => prevLapsStats; }
        public StatsFlags PrevFuelPerLapStatsFlags { get => prevFuelPerLapStats; }
        public StatsFlags RemainingStatsFlags { get => remainingStats; }
        public WheelFlags TyrePresFlags { get => tyrePresFlags; }
        public WheelFlags TyreTempFlags { get => tyreTempFlags; }
        public WheelFlags BrakeTempFlags { get => brakeTempFlags; }

        private StatsFlags prevLapsStats;
        private StatsFlags prevFuelPerLapStats;
        private StatsFlags remainingStats;
        private WheelFlags tyrePresFlags;
        private WheelFlags tyreTempFlags;
        private WheelFlags brakeTempFlags;

        private const string SETTINGS_PATH = @"PluginsData\RaceEngineerPlugin\Settings.json";
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

            NumPreviousValuesStored = s.NumPreviousValuesStored;
            DataLocation = s.DataLocation;
            AccDataLocation = s.AccDataLocation;
            TempColor = s.TempColor;
            TyreTempColorDefValues = s.TyrePresColorDefValues;
            BrakeTempColorDefValues = s.BrakeTempColorDefValues;
            PresColor = s.PresColor;
            TyrePresColorDefValues = s.TyrePresColorDefValues;
            TimeColor = s.TimeColor;
            TimeGraphColor = s.TimeGraphColor;
            TimeColorDeltaValues = s.TimeColorDeltaValues;
            FuelGraphColor = s.FuelGraphColor;
            FuelGraphColorValues = s.FuelGraphColorValues;
            Log = s.Log;
            ShowAllLaps = s.ShowAllLaps;


            ParseLapFlags(s.PrevLapsInfo, ref prevLapsStats, "PrevLapsInfo");
            ParseStatsFlags(s.PrevFuelPerLapInfo, ref prevFuelPerLapStats, "PrevFuelPerLapInfo");
            ParseRemainingStatsFlags(s.RemainingInfo, ref remainingStats, "RemainingInfo");
            ParseWheelFlags(s.TyrePresInfo, ref tyrePresFlags, "TyrePresInfo");
            ParseWheelFlags(s.TyreTempInfo, ref tyreTempFlags, "TyreTempInfo");
            ParseWheelFlags(s.BrakeTempInfo, ref brakeTempFlags, "BrakeTempInfo");

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
            NumPreviousValuesStored = 10;
            DataLocation = "PluginsData\\RaceEngineerPlugin";
            AccDataLocation = "C:\\Users\\" + Environment.UserName + "\\Documents\\Assetto Corsa Competizione";
            TempColor = new string[] { "#87cefa", "#00ff7f", "#00ff7f", "#e60000" };
            TyreTempColorDefValues = new double[] { 70.0, 80.0, 90.0, 100.0 };
            BrakeTempColorDefValues = new double[] { 200.0, 300.0, 500.0, 700.0 };
            PresColor = new string[] { "#87cefa", "#00ff7f", "#00ff7f", "#e60000" };
            TyrePresColorDefValues = new double[] { 26.5, 27.25, 27.75, 28.5 };
            TimeColor = new string[] { "#00ff7f", "#F8F8FF", "#e60000" };
            TimeGraphColor = new string[] { "#00ff7f", "#F8F8FF", "#e60000" };
            TimeColorDeltaValues = new double[] { -1.0, 0.0, 1.0 };
            FuelGraphColor = new string[] { "#00ff7f", "#F8F8FF", "#e60000" };
            FuelGraphColorValues = new double[] { -1.0, 0.0, 1.0 };
            Log = true;
            ShowAllLaps = false;
            PrevLapsInfo = new string[] { "Min", "Max", "Avg", "Std", "Q1", "Median", "Q3"};
            PrevFuelPerLapInfo = new string[] { "Min", "Max", "Avg", "Std", "Q1", "Median", "Q3" };
            TyrePresInfo = new string[] { "Min", "Max", "Avg", "Std", "MinColor", "MaxColor", "AvgColor", "Color" };
            TyreTempInfo = new string[] { "Min", "Max", "Avg", "Std", "MinColor", "MaxColor", "AvgColor", "Color" };
            BrakeTempInfo = new string[] { "Min", "Max", "Avg", "Std", "MinColor", "MaxColor", "AvgColor", "Color" };
            RemainingInfo = new string[] { "Min", "Max", "Avg" };
        }
    }
}