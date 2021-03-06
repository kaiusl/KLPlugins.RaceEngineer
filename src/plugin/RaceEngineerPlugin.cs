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
using KLPlugins.RaceEngineer.Deque;
using GameReaderCommon.Enums;
using ksBroadcastingNetwork;

namespace KLPlugins.RaceEngineer {
    [PluginDescription("Plugin to analyze race data and derive some useful results")]
    [PluginAuthor("Kaius Loos")]
    [PluginName("RaceEngineerPlugin")]
    public class RaceEngineerPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {
        public const string PluginName = "RACE ENGINEER";

        public RaceEngineerPluginSettings ShSettings;
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "Race Engineer Plugin";

        public static readonly Settings Settings = new Settings();
        public static Game Game; // Const during the lifetime of this plugin, plugin is rebuilt at game change
        public static string GameDataPath; // Same as above
        public static string PluginStartTime = $"{DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss")}";
        public static string DefColor = "#555555";

        private static FileStream _logFile;
        private static StreamWriter _logWriter;
        private static bool _isLogFlushed = false;

        private Values _values;

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
            if (!Game.IsAcc) { return; } // ATM only support ACC, some parts could probably work with other games but not tested yet, so let's be safe for now

            if (data.GameRunning && data.OldData != null && data.NewData != null) {
                //var swatch = Stopwatch.StartNew();

                _values.OnDataUpdate(data);

                //swatch.Stop();
                //TimeSpan ts = swatch.Elapsed;
                //File.AppendAllText($"{SETTINGS.DataLocation}\\Logs\\timings\\RETiming_DataUpdate_{pluginStartTime}.txt", $"{ts.TotalMilliseconds}, {BoolToInt(values.booleans.NewData.IsInMenu)}, {BoolToInt(values.booleans.NewData.IsOnTrack)}, {BoolToInt(values.booleans.NewData.IsInPitLane)}, {BoolToInt(values.booleans.NewData.IsInPitBox)}, {BoolToInt(values.booleans.NewData.HasFinishedLap)}\n");
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
            this.SaveCommonSettings("GeneralSettings", ShSettings);
            _values.Dispose();
            _logWriter.Dispose();
            _logFile.Dispose();
            _logWriter = null;
            _logFile = null;
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) {
            return null;//new SettingsControlDemo(this);
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager) {
            var gameName = (string)pluginManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("CurrentGame");
            if (gameName != Game.AccName) return;

            if (Settings.Log) {
                var fpath = $"{Settings.DataLocation}\\Logs\\RELog_{PluginStartTime}.txt";
                Directory.CreateDirectory(Path.GetDirectoryName(fpath));
                _logFile = File.Create(fpath);
                _logWriter = new StreamWriter(_logFile);
            }
            PreJit();

            LogInfo("Starting plugin");
            ShSettings = this.ReadCommonSettings<RaceEngineerPluginSettings>("GeneralSettings", () => new RaceEngineerPluginSettings());

            // DataCorePlugin should be built before, thus this property should be available.

            Game = new Game(gameName);
            GameDataPath = $@"{Settings.DataLocation}\{gameName}";
            _values = new Values();

            pluginManager.GameStateChanged += _values.OnGameStateChanged;
            pluginManager.GameStateChanged += (bool running, PluginManager _) => {
                LogInfo($"GameStateChanged to {running}");
                if (!running) {
                    if (_logWriter != null && !_isLogFlushed) {
                        _logWriter.Flush();
                        _isLogFlushed = true;
                    }
                }
            };

            #region ADD DELEGATES

            this.AttachDelegate("TimeOfDay", () => TimeSpan.FromSeconds(_values.RawData.NewData.Graphics.clock));
            this.AttachDelegate("Session.TimeMultiplier", () => _values.Session.TimeMultiplier);
            this.AttachDelegate("Session.Type", () => _values.Session.RaceSessionType);

            this.AttachDelegate("Tyres.CurrentSet", () => _values.Car.Tyres.CurrentTyreSet);
            this.AttachDelegate("Tyres.CurrentSetLaps", () => _values.Car.Tyres.GetCurrentSetLaps());

            this.AttachDelegate("Weather.Report", () => _values.Weather.WeatherSummary);
            this.AttachDelegate("Weather.AirTemp", () => _values.Weather.AirTemp);
            this.AttachDelegate("Weather.TrackTemp", () => _values.Weather.TrackTemp);
            this.AttachDelegate("Weather.AirTempAtLapStart", () => _values.Weather.AirTempAtLapStart);
            this.AttachDelegate("Weather.TrackTempAtLapStart", () => _values.Weather.TrackTempAtLapStart);

            this.AttachDelegate("Stint.Nr", () => _values.Laps.StintNr);
            this.AttachDelegate("Stint.Laps", () => _values.Laps.StintLaps);

            this.AttachDelegate("Brakes.CurrentSetLaps", () => _values.Car.Brakes.LapsNr);
            this.AttachDelegate("Brakes.CurrentSet", () => _values.Car.Brakes.SetNr);

            this.AttachDelegate("Brakes.DuctFront", () => _values.Car.Setup?.advancedSetup.aeroBalance.brakeDuct[0]);
            this.AttachDelegate("Brakes.DuctRear", () => _values.Car.Setup?.advancedSetup.aeroBalance.brakeDuct[1]);

            this.AttachDelegate("Booleans.IsInMenu", () => _values.Booleans.NewData.IsInMenu ? 1 : 0);
            this.AttachDelegate("Booleans.IsOnTrack", () => _values.Booleans.NewData.IsOnTrack ? 1 : 0);
            this.AttachDelegate("Booleans.IsMoving", () => _values.Booleans.NewData.IsMoving ? 1 : 0);
            this.AttachDelegate("Booleans.IsValidFuelLap", () => _values.Booleans.NewData.IsValidFuelLap ? 1 : 0);
            this.AttachDelegate("Booleans.IsSetupMenuVisible", () => _values.Booleans.NewData.IsSetupMenuVisible ? 1 : 0);
            this.AttachDelegate("Booleans.IsTimeLimitedSession", () => _values.Booleans.NewData.IsTimeLimitedSession ? 1 : 0);
            this.AttachDelegate("Booleans.IsLapLimitedSession", () => _values.Booleans.NewData.IsLapLimitedSession ? 1 : 0);
            this.AttachDelegate("Booleans.IsOutLap", () => _values.Booleans.NewData.IsOutLap ? 1 : 0);
            this.AttachDelegate("Booleans.IsInLap", () => _values.Booleans.NewData.IsInLap ? 1 : 0);
            this.AttachDelegate("Booleans.EcuMapChangedThisLap", () => _values.Booleans.NewData.EcuMapChangedThisLap ? 1 : 0);
            this.AttachDelegate("Booleans.RainIntensityChangedThisLap", () => _values.Booleans.NewData.RainIntensityChangedThisLap ? 1 : 0);
            //this.AttachDelegate("Booleans.IsBroadcastClientConnected", () => _values.RawData.BroadcastClient?.IsConnected ?? false ? 1 : 0);

            this.AttachDelegate("Fuel.Remaining", () => _values.Car.Fuel.Remaining);
            this.AttachDelegate("Fuel.RemainingAtLapStart", () => _values.Car.Fuel.RemainingAtLapStart);

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

            addStats("Laps.Time", _values.Laps.PrevTimes.Stats, Settings.PrevLapsStatsFlags);
            addStats("Fuel.UsedPerLap", _values.Car.Fuel.PrevUsedPerLap.Stats, Settings.PrevFuelPerLapStatsFlags);
            addStats("Fuel.LapsRemaining", _values.RemainingOnFuel.Laps, Settings.RemainingStatsFlags);
            addStats("Fuel.TimeRemaining", _values.RemainingOnFuel.Time, Settings.RemainingStatsFlags);
            addStats("Session.LapsRemaining", _values.RemainingInSession.Laps, Settings.RemainingStatsFlags);
            addStats("Session.TimeRemaining", _values.RemainingInSession.Time, Settings.RemainingStatsFlags);
            addStats("Fuel.NeededInSession", _values.RemainingInSession.FuelNeeded, Settings.RemainingStatsFlags);


            Action<string, double[]> addTyres = (name, values) => {
                this.AttachDelegate(name + Car.Tyres.Names[0], () => values[0]);
                this.AttachDelegate(name + Car.Tyres.Names[1], () => values[1]);
                this.AttachDelegate(name + Car.Tyres.Names[2], () => values[2]);
                this.AttachDelegate(name + Car.Tyres.Names[3], () => values[3]);
            };

            addTyres("Tyres.IdealInputPres", _values.Car.Tyres.IdealInputPres);
            addTyres("Tyres.PredictedIdealInputPresDry", _values.Car.Tyres.PredictedIdealInputPresDry);
            addTyres("Tyres.PredictedIdealInputPresWet", _values.Car.Tyres.PredictedIdealInputPresNowWet);
            addTyres("Tyres.PredictedIdealInputPresIn30MinWet", _values.Car.Tyres.PredictedIdealInputPresFutureWet);
            addTyres("Tyres.CurrentInputPres", _values.Car.Tyres.CurrentInputPres);
            addTyres("Tyres.PresLoss", _values.Car.Tyres.PresLoss);

            Action<string, string[], WheelFlags> addTyresColor = (name, values, flag) => {
                if ((WheelFlags.Color & flag) != 0) {
                    this.AttachDelegate(name + Car.Tyres.Names[0] + "Color", () => values[0]);
                    this.AttachDelegate(name + Car.Tyres.Names[1] + "Color", () => values[1]);
                    this.AttachDelegate(name + Car.Tyres.Names[2] + "Color", () => values[2]);
                    this.AttachDelegate(name + Car.Tyres.Names[3] + "Color", () => values[3]);
                }
            };

            addTyresColor("Tyres.Pres", _values.Car.Tyres.PresColor, Settings.TyrePresFlags);
            addTyresColor("Tyres.Temp", _values.Car.Tyres.TempColor, Settings.TyreTempFlags);
            addTyresColor("Brakes.Temp", _values.Car.Brakes.TempColor, Settings.BrakeTempFlags);

            Action<string, string[], string> addTyreStatsColors = (name, values, statname) => {
                this.AttachDelegate(name + Car.Tyres.Names[0] + statname + "Color", () => values[0]);
                this.AttachDelegate(name + Car.Tyres.Names[1] + statname + "Color", () => values[1]);
                this.AttachDelegate(name + Car.Tyres.Names[2] + statname + "Color", () => values[2]);
                this.AttachDelegate(name + Car.Tyres.Names[3] + statname + "Color", () => values[3]);
            };

            Action<string, Stats.WheelsStats, string[], string[], string[], WheelFlags> addTyresStats = (name, values, minC, maxC, avgC, flags) => {
                void _addStats(string n, Stats.Stats v) {
                    if ((WheelFlags.Min & flags) != 0) {
                        this.AttachDelegate(n + "Min", () => v.Min);
                    }
                    if ((WheelFlags.Max & flags) != 0) {
                        this.AttachDelegate(n + "Max", () => v.Max);
                    }
                    if ((WheelFlags.Avg & flags) != 0) {
                        this.AttachDelegate(n + "Avg", () => v.Avg);
                    }
                    if ((WheelFlags.Std & flags) != 0) {
                        this.AttachDelegate(n + "Std", () => v.Std);
                    }
                }
                _addStats(name + Car.Tyres.Names[0], values[0]);
                _addStats(name + Car.Tyres.Names[1], values[1]);
                _addStats(name + Car.Tyres.Names[2], values[2]);
                _addStats(name + Car.Tyres.Names[3], values[3]);

                if ((WheelFlags.MinColor & Settings.TyrePresFlags) != 0) {
                    addTyreStatsColors(name, minC, "Min");
                }

                if ((WheelFlags.MaxColor & Settings.TyrePresFlags) != 0) {
                    addTyreStatsColors(name, maxC, "Max");
                }

                if ((WheelFlags.AvgColor & Settings.TyrePresFlags) != 0) {
                    addTyreStatsColors(name, avgC, "Avg");
                }

            };

            addTyresStats("Tyres.PresOverLap", _values.Car.Tyres.PresOverLap, _values.Car.Tyres.PresColorMin, _values.Car.Tyres.PresColorMax, _values.Car.Tyres.PresColorAvg, Settings.TyrePresFlags);
            addTyresStats("Tyres.TempOverLap", _values.Car.Tyres.TempOverLap, _values.Car.Tyres.TempColorMin, _values.Car.Tyres.TempColorMax, _values.Car.Tyres.TempColorAvg, Settings.TyreTempFlags);
            addTyresStats("Brakes.TempOverLap", _values.Car.Brakes.TempOverLap, _values.Car.Brakes.TempColorMin, _values.Car.Brakes.TempColorMax, _values.Car.Brakes.TempColorAvg, Settings.BrakeTempFlags);



            Action<string, FixedSizeDequeStats> addPrevData = (name, values) => {
                if (Settings.NumPreviousValuesStored > 0) this.AttachDelegate(name + "0", () => values[0]);
                if (Settings.NumPreviousValuesStored > 1) this.AttachDelegate(name + "1", () => values[1]);
                if (Settings.NumPreviousValuesStored > 2) this.AttachDelegate(name + "2", () => values[2]);
                if (Settings.NumPreviousValuesStored > 3) this.AttachDelegate(name + "3", () => values[3]);
                if (Settings.NumPreviousValuesStored > 4) this.AttachDelegate(name + "4", () => values[4]);
                if (Settings.NumPreviousValuesStored > 5) this.AttachDelegate(name + "5", () => values[5]);
                if (Settings.NumPreviousValuesStored > 6) this.AttachDelegate(name + "6", () => values[6]);
                if (Settings.NumPreviousValuesStored > 7) this.AttachDelegate(name + "7", () => values[7]);
                if (Settings.NumPreviousValuesStored > 8) this.AttachDelegate(name + "8", () => values[8]);
                if (Settings.NumPreviousValuesStored > 9) this.AttachDelegate(name + "9", () => values[9]);
                if (Settings.NumPreviousValuesStored > 10) this.AttachDelegate(name + "10", () => values[10]);
                if (Settings.NumPreviousValuesStored > 11) this.AttachDelegate(name + "11", () => values[11]);
                if (Settings.NumPreviousValuesStored > 12) this.AttachDelegate(name + "12", () => values[12]);
                if (Settings.NumPreviousValuesStored > 13) this.AttachDelegate(name + "13", () => values[13]);
                if (Settings.NumPreviousValuesStored > 14) this.AttachDelegate(name + "14", () => values[14]);
                if (Settings.NumPreviousValuesStored > 15) this.AttachDelegate(name + "15", () => values[15]);
                if (Settings.NumPreviousValuesStored > 16) this.AttachDelegate(name + "16", () => values[16]);
                if (Settings.NumPreviousValuesStored > 17) this.AttachDelegate(name + "17", () => values[17]);
                if (Settings.NumPreviousValuesStored > 18) this.AttachDelegate(name + "18", () => values[18]);
                if (Settings.NumPreviousValuesStored > 19) this.AttachDelegate(name + "19", () => values[19]);
                if (Settings.NumPreviousValuesStored > 20) this.AttachDelegate(name + "20", () => values[20]);
                if (Settings.NumPreviousValuesStored > 21) this.AttachDelegate(name + "21", () => values[21]);
                if (Settings.NumPreviousValuesStored > 22) this.AttachDelegate(name + "22", () => values[22]);
                if (Settings.NumPreviousValuesStored > 23) this.AttachDelegate(name + "23", () => values[23]);
                if (Settings.NumPreviousValuesStored > 24) this.AttachDelegate(name + "24", () => values[24]);
                if (Settings.NumPreviousValuesStored > 25) this.AttachDelegate(name + "25", () => values[25]);
                if (Settings.NumPreviousValuesStored > 26) this.AttachDelegate(name + "26", () => values[26]);
                if (Settings.NumPreviousValuesStored > 27) this.AttachDelegate(name + "27", () => values[27]);
                if (Settings.NumPreviousValuesStored > 28) this.AttachDelegate(name + "28", () => values[28]);
                if (Settings.NumPreviousValuesStored > 29) this.AttachDelegate(name + "29", () => values[29]);
                if (Settings.NumPreviousValuesStored > 30) this.AttachDelegate(name + "30", () => values[30]);
            };

            addPrevData("Laps.PrevTime", _values.Laps.PrevTimes);
            addPrevData("Fuel.PrevUsedPerLap", _values.Car.Fuel.PrevUsedPerLap);

            #endregion

        }

        public static void LogToFile(string msq) {
            if (_logWriter != null) { 
                _logWriter.WriteLine(msq);
                _isLogFlushed = false;
            }
        }

        public static void LogInfo(string msq, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            if (Settings.Log) {
                var pathParts = sourceFilePath.Split('\\');
                SimHub.Logging.Current.Info($"{PluginName} ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}");
                LogToFile($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss")} INFO ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}\n");
            }
        }

        public static void LogWarn(string msq, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            var pathParts = sourceFilePath.Split('\\');
            SimHub.Logging.Current.Warn($"{PluginName} ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}");
            LogToFile($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss")} WARN ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}\n");
        }

        public static void LogError(string msq, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            var pathParts = sourceFilePath.Split('\\');
            SimHub.Logging.Current.Error($"{PluginName} ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}");
            LogToFile($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss")} ERROR ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}\n");
        }

        public static void LogFileSeparator() {
            if (Settings.Log) {
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


    public static class Helpers {

        public static RaceSessionType RaceSessionTypeFromString(string s) {
            switch (s) {
                case "PRACTICE":
                    return RaceSessionType.Practice;
                case "QUALIFY":
                    return RaceSessionType.Qualifying;
                case "RACE":
                    return RaceSessionType.Race;
                case "HOTLAP":
                    return RaceSessionType.Hotlap;
                case "7":
                    return RaceSessionType.Hotstint;
                case "8":
                    return RaceSessionType.HotlapSuperpole;
                default:
                    return RaceSessionType.Practice;

            }
        }

        public static string GetAccCarClass(string name) { 
            name = name.ToLower();
            if (name.Contains("gt4")) {
                return "gt4";
            }

            switch (name) {
                case "porsche_991ii_gt3_cup":
                case "porsche_992_gt3_cup":
                case "ferrari_488_challenge_evo":
                case "lamborghini_huracan_st_evo2":
                case "lamborghini_huracan_st":
                    return "gtc";
                case "bmw_m2_cs_racing":
                    return "tcx";
               
                default:
                    return "gt3";
            }
        }


    }
}