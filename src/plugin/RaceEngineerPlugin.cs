using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Reflection;
using ACSharedMemory.ACC.Reader;

namespace RaceEngineerPlugin {
    [PluginDescription("Plugin to analyze race data and derive some useful results")]
    [PluginAuthor("Kaius Loos")]
    [PluginName("RaceEngineerPlugin")]
    public class RaceEngineerPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {
        public const string PLUGIN_NAME = "RACE ENGINEER";

        public RaceEngineerPluginSettings Settings;
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "Race Engineer Plugin";

        private const string PREV_LAPTIME_PROP_NAME = "PrevLapTime";
        private const string PREV_FUEL_PROP_NAME = "PrevFuelPerLap";
        private const string TYRE_PRES_PROP_NAME = "TyrePres";
        private const string TYRE_TEMP_PROP_NAME = "TyreTemp";
        private const string BRAKE_TEMP_PROP_NAME = "BrakeTemp";
        private string[] tyreNames = new string[4] { "FL", "FR", "RL", "RR" };

        public static readonly Settings SETTINGS = new Settings();
        public static Game.Game GAME; // Const during the lifetime of this plugin, plugin is rebuilt at game change
        public static string GAME_PATH; // Same as above
        private static FileStream f;
        private static StreamWriter sw;
        private static bool flushed = false;

        public static string pluginStartTime = $"{DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss")}";

        private Stopwatch swatch = new Stopwatch();
        private Values values;

        /// <summary>
        /// Called one time per game data update, contains all normalized game data, 
        /// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        /// 
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        /// 
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data"></param>
        public void DataUpdate(PluginManager pluginManager, ref GameData data) {
            if (!GAME.IsACC) { return; } // ATM only support ACC, some parts could probably work with other games but not tested yet, so let's be safe for now

            if (data.GameRunning) {
                if (data.OldData != null && data.NewData != null) {
                    swatch.Restart();
  
                    values.OnDataUpdate(data);

                    //if (!rawPrinted && values.booleans.NewData.ExitedMenu) {
                    //    var raw = data.NewData.GetRawDataObject();
                    //    //var acc_RootData = JsonConvert.SerializeObject(raw, Formatting.Indented);
                    //    //File.WriteAllText($"{SETTINGS.DataLocation}\\AccRawData.json", acc_RootData);
                    //    //rawPrinted = true;
                    //    var parsed = (ACCRawData)raw;
                    //}


                    if (values.booleans.NewData.HasFinishedLap) {
                        for (int i = 0; i < values.car.Fuel.PrevUsedPerLap.Count; i++) {
                            double lapTime = values.laps.PrevTimes[i];
                            var istr = i.ToString();

                            pluginManager.SetPropertyValue(PREV_LAPTIME_PROP_NAME + istr, this.GetType(), lapTime);
                            if ((LapFlags.TimeDeltaToAvg & SETTINGS.LapFlags) != 0) {
                                pluginManager.SetPropertyValue(PREV_LAPTIME_PROP_NAME + "DeltaToAvg" + istr, this.GetType(), lapTime - values.laps.PrevTimes.Avg);
                            }
                            pluginManager.SetPropertyValue(PREV_FUEL_PROP_NAME + istr, this.GetType(), values.car.Fuel.PrevUsedPerLap[i]);
                        }
                    }
                    swatch.Stop();
                    TimeSpan ts = swatch.Elapsed;
                    File.AppendAllText($"{SETTINGS.DataLocation}\\Logs\\timings\\RETiming_DataUpdate_{pluginStartTime}.txt", $"{ts.TotalMilliseconds}, {BoolToInt(values.booleans.NewData.IsInMenu)}, {BoolToInt(values.booleans.NewData.IsOnTrack)}, {BoolToInt(values.booleans.NewData.IsInPitLane)}, {BoolToInt(values.booleans.NewData.IsInPitBox)}, {BoolToInt(values.booleans.NewData.HasFinishedLap)}\n");
                }
            } else {
                values.OnGameNotRunning();
                if (sw != null && !flushed) {
                    sw.Flush();
                    flushed = true;
                }
            }
        }

        private int BoolToInt(bool b) { 
            return b ? 1 : 0;
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager) {
            this.SaveCommonSettings("GeneralSettings", Settings);
            values.Dispose();
            sw.Dispose();
            f.Dispose();
            RaceEngineerPlugin.LogInfo("Disposed.");
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) {
            return new SettingsControlDemo(this);
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager) {
            if (SETTINGS.Log) {
                var fpath = $"{SETTINGS.DataLocation}\\Logs\\RELog_{pluginStartTime}.txt";
                Directory.CreateDirectory(Path.GetDirectoryName(fpath));
                f = File.Create(fpath);
                sw = new StreamWriter(f);
            }
            PreJit();

            LogInfo("Starting plugin");
            Settings = this.ReadCommonSettings<RaceEngineerPluginSettings>("GeneralSettings", () => new RaceEngineerPluginSettings());

            // DataCorePlugin should be built before, thus this property should be available.
            var gameName = (string)pluginManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("CurrentGame");
            GAME = new Game.Game(gameName);
            GAME_PATH = $@"{SETTINGS.DataLocation}\{gameName}";
            values = new Values();

            #region ADD DELEGATES

            this.AttachDelegate("DBG_currentTyreSet", () => values.car.Tyres.currentTyreSet);
            this.AttachDelegate("DBG_weatherReport", () => values.weather.weatherSummary);

            this.AttachDelegate("IsInMenu", () => values.booleans.NewData.IsInMenu ? 1 : 0);

            this.AttachDelegate("FuelLeft", () => values.car.Fuel.Remaining);
            this.AttachDelegate("IsOnTrack", () => values.booleans.NewData.IsOnTrack ? 1 : 0);
            this.AttachDelegate("IsValidFuelLap", () => values.booleans.NewData.IsValidFuelLap ? 1 : 0);

            Action<string, Stats.Stats, StatsFlags> addStats = (name, values, settings) => {
                if ((StatsFlags.Min & settings) != 0) {
                    this.AttachDelegate(name + "Min", () => values.Min);
                }
                if ((StatsFlags.Max & settings) != 0) {
                    this.AttachDelegate(name + "Max", () => values.Max);
                }
                if ((StatsFlags.Avg & settings) != 0) {
                    this.AttachDelegate(name + "Avg", () => values.Avg);
                }
                if ((StatsFlags.Std & settings) != 0) {
                    this.AttachDelegate(name + "Std", () => values.Std);
                }
                if ((StatsFlags.Median & settings) != 0) {
                    this.AttachDelegate(name + "Median", () => values.Median);
                }
                if ((StatsFlags.Q1 & settings) != 0) {
                    this.AttachDelegate(name + "Q1", () => values.Q1);
                }
                if ((StatsFlags.Q3 & settings) != 0) {
                    this.AttachDelegate(name + "Q3", () => values.Q3);
                }
            };

            addStats("LapTime", values.laps.PrevTimes.Stats, SETTINGS.PrevLapsStatsFlags);
            addStats("FuelPerLap", values.car.Fuel.PrevUsedPerLap.Stats, SETTINGS.PrevFuelPerLapStatsFlags);
            addStats("LapsRemainingOnFuel", values.remainingOnFuel.laps, SETTINGS.RemainingStatsFlags);
            addStats("TimeRemainingOnFuel", values.remainingOnFuel.time, SETTINGS.RemainingStatsFlags);
            addStats("LapsRemainingInSession", values.remainingInSession.laps, SETTINGS.RemainingStatsFlags);
            addStats("TimeRemainingInSession", values.remainingInSession.time, SETTINGS.RemainingStatsFlags);
            addStats("FuelNeededInSession", values.remainingInSession.fuelNeeded, SETTINGS.RemainingStatsFlags);


            Action<string, double[]> addTyres = (name, values) => {
                this.AttachDelegate(name + tyreNames[0], () => values[0]);
                this.AttachDelegate(name + tyreNames[1], () => values[1]);
                this.AttachDelegate(name + tyreNames[2], () => values[2]);
                this.AttachDelegate(name + tyreNames[3], () => values[3]);
            };
           
            addTyres("IdealInputTyrePres", values.car.Tyres.IdealInputPres);
            addTyres("PredictedIdealInputTyrePresDry", values.car.Tyres.PredictedIdealInputPresDry);
            addTyres("PredictedIdealInputTyrePresWet", values.car.Tyres.PredictedIdealInputPresNowWet);
            addTyres("PredictedIdealInputTyrePresIn30MinWet", values.car.Tyres.PredictedIdealInputPresFutureWet);
            addTyres("CurrentInputTyrePres", values.car.Tyres.CurrentInputPres);
            addTyres("TyrePresLoss", values.car.Tyres.PresLoss);

            Action<string, string[], WheelFlags> addTyresColor = (name, values, flag) => {
                if ((WheelFlags.Color & flag) != 0) {
                    this.AttachDelegate(name + tyreNames[0] + "Color", () => values[0]);
                    this.AttachDelegate(name + tyreNames[1] + "Color", () => values[1]);
                    this.AttachDelegate(name + tyreNames[2] + "Color", () => values[2]);
                    this.AttachDelegate(name + tyreNames[3] + "Color", () => values[3]);
                }
            };

            addTyresColor("TyrePres", values.car.Tyres.PresColor, SETTINGS.TyrePresFlags);
            addTyresColor("TyreTemp", values.car.Tyres.TempColor, SETTINGS.TyreTempFlags);
            addTyresColor("BrakeTemp", values.car.Brakes.TempColor, SETTINGS.BrakeTempFlags);

            Action<string, Stats.Stats, Color.ColorCalculator, WheelFlags> addStatsWColor = (name, v, cc, flags) => {
                if ((WheelFlags.Min & flags) != 0) {
                    this.AttachDelegate(name + "Min", () => v.Min);
                }
                if ((WheelFlags.Max & flags) != 0) {
                    this.AttachDelegate(name + "Max", () => v.Max);
                }
                if ((WheelFlags.Avg & flags) != 0) {
                    this.AttachDelegate(name + "Avg", () => v.Avg);
                }
                if ((WheelFlags.Std & flags) != 0) {
                    this.AttachDelegate(name + "Std", () => v.Std);
                }

                if ((WheelFlags.MinColor & flags) != 0) {
                    this.AttachDelegate(name + "MinColor", () => cc.GetColor(v.Min).ToHEX());
                }
                if ((WheelFlags.MaxColor & flags) != 0) {
                    this.AttachDelegate(name + "MaxColor", () => cc.GetColor(v.Max).ToHEX());
                }
                if ((WheelFlags.AvgColor & flags) != 0) {
                    this.AttachDelegate(name + "AvgColor", () => cc.GetColor(v.Avg).ToHEX());
                }
            };

            Action<string, Stats.WheelsStats, Color.ColorCalculator, Color.ColorCalculator, WheelFlags> addTyresStats = (name, values, ccf, ccr, flags) => {
                addStatsWColor(name + tyreNames[0], values[0], ccf, flags);
                addStatsWColor(name + tyreNames[1], values[1], ccf, flags);
                addStatsWColor(name + tyreNames[2], values[2], ccr, flags);
                addStatsWColor(name + tyreNames[3], values[3], ccr, flags);
            };

            addTyresStats("TyrePresOverLap", values.car.Tyres.PresOverLap, values.car.Tyres.PresColorF, values.car.Tyres.PresColorR, SETTINGS.TyrePresFlags);
            addTyresStats("TyreTempOverLap", values.car.Tyres.TempOverLap, values.car.Tyres.TempColorF, values.car.Tyres.TempColorR, SETTINGS.TyreTempFlags);
            addTyresStats("BrakeTempOverLap", values.car.Brakes.TempOverLap, values.car.Brakes.tempColor, values.car.Brakes.tempColor, SETTINGS.BrakeTempFlags);
            #endregion

            ///////////////////////

            #region ADD PROPERTIES
            // Add some properties where we cannot use delegates

            // Number of available properties depends on runtime conditions (eg how many laps are completed)
            for (int i = 0; i < SETTINGS.NumPreviousValuesStored; i++) {
                var istr = i.ToString();

                pluginManager.AddProperty(PREV_LAPTIME_PROP_NAME + istr, this.GetType(), 0.0);
                if ((LapFlags.TimeDeltaToAvg & SETTINGS.LapFlags) != 0) {
                    pluginManager.AddProperty(PREV_LAPTIME_PROP_NAME + "DeltaToAvg" + istr, this.GetType(), 0.0);
                }
                pluginManager.AddProperty(PREV_FUEL_PROP_NAME + istr, this.GetType(), 0.0);
            }
            #endregion

        }

        public static void LogToFile(string msq) {
            if (f != null) { 
                sw.WriteLine(msq);
                flushed = false;
            }
        }

        public static void LogInfo(string msq, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            if (SETTINGS.Log) {
                var pathParts = sourceFilePath.Split('\\');
                SimHub.Logging.Current.Info($"{PLUGIN_NAME} ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}");
                LogToFile($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss")} INFO ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}\n");
            }
        }

        public static void LogWarn(string msq, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            var pathParts = sourceFilePath.Split('\\');
            SimHub.Logging.Current.Warn($"{PLUGIN_NAME} ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}");
            LogToFile($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss")} WARN ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}\n");
        }

        public static void LogError(string msq, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            var pathParts = sourceFilePath.Split('\\');
            SimHub.Logging.Current.Error($"{PLUGIN_NAME} ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}");
            LogToFile($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss")} ERROR ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}\n");
        }

        public static void LogFileSeparator() {
            if (SETTINGS.Log) {
                LogToFile("\n----------------------------------------------------------\n");
            }
        }

        static void PreJit() {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var types = Assembly.GetExecutingAssembly().GetTypes();//new Type[] { typeof(Database.Database) };
            foreach (var type in types) {
                foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Public | BindingFlags.Instance |
                                    BindingFlags.Static)) {
                    if ((method.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract || method.ContainsGenericParameters) {
                        continue;
                    }
                    System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(method.MethodHandle);
                }
            }

            var t = sw.Elapsed;
            LogInfo($"Prejit finished in {t.TotalMilliseconds}ms");
        }

    }
}