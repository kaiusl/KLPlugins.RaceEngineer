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
        private string[] tyreNames = new string[4] { "LF", "RF", "LR", "RR" };

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
            if (GAME.IsUnknown) { return; } // Unknown game is running, do nothing

            if (data.GameRunning) {
                if (data.OldData != null && data.NewData != null) {
                    //swatch.Restart();

                    values.OnDataUpdate(pluginManager, data);

                    #region UPDATE PROPERTIES

                    if (values.booleans.NewData.HasFinishedLap) {
                        for (int i = 0; i < values.car.Fuel.PrevUsedPerLap.Count; i++) {
                            double lapTime = values.laps.PrevTimes[i];
                            var istr = i.ToString();

                            //pluginManager.SetPropertyValue(PREV_LAPTIME_PROP_NAME + istr, this.GetType(), fromSeconds(lapTime));
                            pluginManager.SetPropertyValue(PREV_LAPTIME_PROP_NAME + istr, this.GetType(), lapTime);
                            if ((LapFlags.TimeDeltaToAvg & SETTINGS.LapFlags) != 0) {
                                pluginManager.SetPropertyValue(PREV_LAPTIME_PROP_NAME + "DeltaToAvg" + istr, this.GetType(), lapTime - values.laps.PrevTimes.Avg);
                            }
                            pluginManager.SetPropertyValue(PREV_FUEL_PROP_NAME + istr, this.GetType(), values.car.Fuel.PrevUsedPerLap[i]);
                        }
                    }
                    #endregion

                    //swatch.Stop();
                    //TimeSpan ts = swatch.Elapsed;
                    //File.AppendAllText($"{SETTINGS.DataLocation}\\Logs\\timings\\RETiming_DataUpdate_{pluginStartTime}.txt", $"{ts.TotalMilliseconds}, {BoolToInt(values.booleans.NewData.IsInMenu)}, {BoolToInt(values.booleans.NewData.IsOnTrack)}, {BoolToInt(values.booleans.NewData.IsInPitLane)}, {BoolToInt(values.booleans.NewData.IsInPitBox)}, {BoolToInt(values.booleans.NewData.HasFinishedLap)}\n");
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
            // Save settings
            RaceEngineerPlugin.LogInfo("Disposed.");
            this.SaveCommonSettings("GeneralSettings", Settings);
            values.Dispose();
            sw.Dispose();
            f.Dispose();
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
            var gameName = (string)pluginManager.GetPropertyValue("DataCorePlugin.CurrentGame");
            GAME = new Game.Game(gameName);
            GAME_PATH = $@"{SETTINGS.DataLocation}\{gameName}";
            values = new Values();

            #region ADD DELEGATES

            this.AttachDelegate("DBG_currentTyreSet", () => values.car.Tyres.currentTyreSet);

            this.AttachDelegate("IsInMenu", () => values.booleans.NewData.IsInMenu);

            //this.AttachDelegate("FuelLeft", () => values.car.Fuel.Remaining);
            this.AttachDelegate("IsOnTrack", () => values.booleans.NewData.IsOnTrack);
            this.AttachDelegate("IsValidFuelLap", () => values.booleans.NewData.IsValidFuelLap);

            this.AttachDelegate("BAirTemp", () => values.realtimeUpdate?.AmbientTemp);
            this.AttachDelegate("BTrackTemp", () => values.realtimeUpdate?.TrackTemp);
            this.AttachDelegate("BSessionPhase", () => values.realtimeUpdate?.Phase.ToString());
            this.AttachDelegate("BSessionTime", () => values.realtimeUpdate?.SessionTime);
            this.AttachDelegate("BRemainingTime", () => values.realtimeUpdate?.RemainingTime);
            this.AttachDelegate("BTimeOfDay", () => values.realtimeUpdate?.TimeOfDay);
            this.AttachDelegate("BRainLevel", () => values.realtimeUpdate?.RainLevel);
            this.AttachDelegate("BClouds", () => values.realtimeUpdate?.Clouds);
            this.AttachDelegate("BWetness", () => values.realtimeUpdate?.Wetness);
            this.AttachDelegate("BSessionRemainingTime", () => values.realtimeUpdate?.SessionRemainingTime);
            this.AttachDelegate("BSessionEndTime", () => values.realtimeUpdate?.SessionEndTime);
            this.AttachDelegate("BSessionType", () => values.realtimeUpdate?.SessionType.ToString());


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

            addStats("LapTime", values.laps.PrevTimes.Stats, SETTINGS.PrevLapsStats);
            addStats("FuelPerLap", values.car.Fuel.PrevUsedPerLap.Stats, SETTINGS.PrevFuelPerLapStats);
            addStats("LapsRemainingOnFuel", values.remainingOnFuel.laps, SETTINGS.RemainingStats);
            addStats("TimeRemainingOnFuel", values.remainingOnFuel.time, SETTINGS.RemainingStats);
            addStats("LapsRemainingInSession", values.remainingInSession.laps, SETTINGS.RemainingStats);
            addStats("TimeRemainingInSession", values.remainingInSession.time, SETTINGS.RemainingStats);
            addStats("FuelNeededInSession", values.remainingInSession.fuelNeeded, SETTINGS.RemainingStats);


            Action<string, double[]> addTyres = (name, values) => {
                this.AttachDelegate(name + tyreNames[0], () => values[0]);
                this.AttachDelegate(name + tyreNames[1], () => values[1]);
                this.AttachDelegate(name + tyreNames[2], () => values[2]);
                this.AttachDelegate(name + tyreNames[3], () => values[3]);
            };
           
            addTyres("IdealInputTyrePres", values.car.Tyres.IdealInputPres);
            addTyres("PredictedIdealInputTyrePres", values.car.Tyres.PredictedIdealInputPres);
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

            //////////////////////////////////////////////////////////

            // Declare an event
            this.AddEvent("SpeedWarning");

            // Declare an action which can be called
            this.AddAction("IncrementSpeedWarning", (a, b) => {
                Settings.SpeedWarningLevel++;
            });

            // Declare an action which can be called
            this.AddAction("DecrementSpeedWarning", (a, b) => {
                Settings.SpeedWarningLevel--;
            });
        }

        public static void LogToFile(string msq) {
            if (f != null) { 
                sw.WriteLine(msq);
                flushed = false;
            }
        }

        private TimeSpan fromSeconds(double seconds) {
            if (double.IsNaN(seconds)) {
                return TimeSpan.Zero;
            } else {
                return TimeSpan.FromSeconds(seconds);
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

        public static string TrackGripStatus(PluginManager pm) {
            if (GAME.IsACC) {
                var gs = (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.trackGripStatus");
                string gs_str;
                switch (gs) {
                    case 0: gs_str = "Green"; break;
                    case 1: gs_str = "Fast"; break;
                    case 2: gs_str = "Optimum"; break;
                    case 3: gs_str = "Greasy"; break;
                    case 4: gs_str = "Damp"; break;
                    case 5: gs_str = "Wet"; break;
                    case 6: gs_str = "Flooded"; break;
                    default: gs_str = "Unknown"; break;
                }
                return gs_str;
            } else {
                return "Unknown";
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