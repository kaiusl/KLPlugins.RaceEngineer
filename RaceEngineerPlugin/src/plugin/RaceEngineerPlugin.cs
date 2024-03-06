using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Media;

using GameReaderCommon;

using KLPlugins.RaceEngineer.Car;
using KLPlugins.RaceEngineer.Deque;
using KLPlugins.RaceEngineer.Stats;

using ksBroadcastingNetwork;

using SimHub.Plugins;

namespace KLPlugins.RaceEngineer {
    [PluginDescription("Plugin to analyze race data and derive some useful results")]
    [PluginAuthor("Kaius Loos")]
    [PluginName("RaceEngineerPlugin")]
    public class RaceEngineerPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {
        internal const string PluginName = "RACE ENGINEER";

        // these are set in Init method, 
        // so they are technically null but nothing can touch them before 
        //we actually initialize them, so practically they cannot be nulls
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        internal RaceEngineerPluginSettings ShSettings;
        public PluginManager PluginManager { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "Race Engineer Plugin";

        public static readonly Settings Settings = new();

        // these are set in Init method
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static Game Game { get; private set; } // Const during the lifetime of this plugin, plugin is rebuilt at game change

        internal static string GameDataPath { get; private set; } // Same as above
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        internal static readonly string PluginStartTime = $"{DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss")}";

        private static FileStream? _logFile;
        private static StreamWriter? _logWriter;
        private static bool _isLogFlushed = false;

        // these are set in Init method
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Values Values { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

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
            //if (!Game.IsAcc) { return; } // ATM only support ACC, some parts could probably work with other games but not tested yet, so let's be safe for now

            if (data.GameRunning && data.OldData != null && data.NewData != null) {
                //var swatch = Stopwatch.StartNew();

                this.Values.OnDataUpdate(data);

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
            this.SaveCommonSettings("GeneralSettings", this.ShSettings);
            this.Values?.Dispose();
            if (_logWriter != null) {
                _logWriter.Dispose();
                _logWriter = null;
            }

            if (_logFile != null) {
                _logFile.Dispose();
                _logFile = null;
            }
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control? GetWPFSettingsControl(PluginManager pluginManager) {
            return null;//new SettingsControlDemo(this);
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager) {
            this.PluginManager = pluginManager;
            var gameName = (string)pluginManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("CurrentGame");
            Game = new Game(gameName);
            // if (gameName != Game.AccName) return;

            if (Settings.Log) {
                var fpath = $"{Settings.DataLocation}\\Logs\\RELog_{PluginStartTime}.txt";
                Directory.CreateDirectory(Path.GetDirectoryName(fpath));
                _logFile = File.Create(fpath);
                _logWriter = new StreamWriter(_logFile);
            }
            PreJit();

            LogInfo("Starting plugin");
            this.ShSettings = this.ReadCommonSettings<RaceEngineerPluginSettings>("GeneralSettings", () => new RaceEngineerPluginSettings());

            // DataCorePlugin should be built before, thus this property should be available.


            GameDataPath = $@"{Settings.DataLocation}\{gameName}";
            this.Values = new Values(pluginManager);

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

            const string NORMALIZED_KEYWORD = "Norm";
            const string MIN_KEYWORD = "Min";
            const string AVG_KEYWORD = "Avg";
            const string MAX_KEYWORD = "Max";
            const string STD_KEYWORD = "Std";

            this.AttachDelegate("TimeOfDay", () => TimeSpan.FromSeconds(this.Values.Session.TimeOfDay));
            this.AttachDelegate("Session.Name.Pretty", () => SessionTypeMethods.ToPrettyString(this.Values.Session.SessionType));
            this.AttachDelegate("Session.TimeMultiplier", () => this.Values.Session.TimeMultiplier);
            this.AttachDelegate("Session.Type", () => this.Values.Session.SessionType);
            this.AttachDelegate("Session.IsRace", () => this.Values.Session.SessionType == SessionType.Race);
            this.AttachDelegate("Session.IsPractice", () => this.Values.Session.SessionType == SessionType.Practice);
            this.AttachDelegate("Session.IsQualifying", () => this.Values.Session.SessionType == SessionType.Qualifying);
            this.AttachDelegate("Session.IsHotlap", () => this.Values.Session.SessionType == SessionType.Hotlap);
            this.AttachDelegate("Session.IsHotstint", () => this.Values.Session.SessionType == SessionType.Hotstint);
            this.AttachDelegate("Session.IsHotlapSuperpole", () => this.Values.Session.SessionType == SessionType.HotlapSuperpole);
            this.AttachDelegate("Session.IsDrift", () => this.Values.Session.SessionType == SessionType.Drift);
            this.AttachDelegate("Session.IsTimeAttack", () => this.Values.Session.SessionType == SessionType.TimeAttack);
            this.AttachDelegate("Session.IsTimeTrial", () => this.Values.Session.SessionType == SessionType.TimeTrial);
            this.AttachDelegate("Session.IsWarmup", () => this.Values.Session.SessionType == SessionType.Warmup);

            this.AttachDelegate("Tyres.CurrentSet", () => this.Values.Car.Tyres.CurrentTyreSet);
            this.AttachDelegate("Tyres.CurrentSetLaps", () => this.Values.Car.Tyres.GetCurrentSetLaps());


            void addIdealRange<T>(string name, MinMaxAvg<T> value) {
                this.AttachDelegate(name + '.' + MIN_KEYWORD, () => value.Min);
                this.AttachDelegate(name + '.' + AVG_KEYWORD, () => value.Avg);
                this.AttachDelegate(name + '.' + MAX_KEYWORD, () => value.Max);
            }

            addIdealRange("Tyres.Pres.Ideal.1", this.Values.Car.Tyres.Info.IdealPresRange.F);
            addIdealRange("Tyres.Pres.Ideal.2", this.Values.Car.Tyres.Info.IdealPresRange.R);
            addIdealRange("Tyres.Temp.Ideal.1", this.Values.Car.Tyres.Info.IdealTempRange.F);
            addIdealRange("Tyres.Temp.Ideal.2", this.Values.Car.Tyres.Info.IdealTempRange.R);

            this.AttachDelegate("Tyres.ShortName", () => this.Values.Car.Tyres.Info.ShortName);

            this.AttachDelegate("Weather.Report", () => this.Values.Weather.WeatherSummary);
            this.AttachDelegate("Weather.AirTemp", () => this.Values.Weather.AirTemp);
            this.AttachDelegate("Weather.TrackTemp", () => this.Values.Weather.TrackTemp);
            this.AttachDelegate("Weather.AirTempAtLapStart", () => this.Values.Weather.AirTempAtLapStart);
            this.AttachDelegate("Weather.TrackTempAtLapStart", () => this.Values.Weather.TrackTempAtLapStart);

            this.AttachDelegate("Stint.Nr", () => this.Values.Laps.StintNr);
            this.AttachDelegate("Stint.Laps", () => this.Values.Laps.StintLaps);

            this.AttachDelegate("Brakes.CurrentSetLaps", () => this.Values.Car.Brakes.LapsNr);
            this.AttachDelegate("Brakes.CurrentSet", () => this.Values.Car.Brakes.SetNr);

            this.AttachDelegate("Brakes.DuctFront", () => this.Values.Car.Setup?.advancedSetup.aeroBalance.brakeDuct[0]);
            this.AttachDelegate("Brakes.DuctRear", () => this.Values.Car.Setup?.advancedSetup.aeroBalance.brakeDuct[1]);

            this.AttachDelegate("Booleans.IsInMenu", () => this.Values.Booleans.NewData.IsInMenu ? 1 : 0);
            this.AttachDelegate("Booleans.IsOnTrack", () => this.Values.Booleans.NewData.IsOnTrack ? 1 : 0);
            this.AttachDelegate("Booleans.IsMoving", () => this.Values.Booleans.NewData.IsMoving ? 1 : 0);
            this.AttachDelegate("Booleans.IsValidFuelLap", () => this.Values.Booleans.NewData.IsValidFuelLap ? 1 : 0);
            this.AttachDelegate("Booleans.IsSetupMenuVisible", () => this.Values.Booleans.NewData.IsSetupMenuVisible ? 1 : 0);
            this.AttachDelegate("Booleans.IsTimeLimitedSession", () => this.Values.Booleans.NewData.IsTimeLimitedSession ? 1 : 0);
            this.AttachDelegate("Booleans.IsLapLimitedSession", () => this.Values.Booleans.NewData.IsLapLimitedSession ? 1 : 0);
            this.AttachDelegate("Booleans.IsOutLap", () => this.Values.Booleans.NewData.IsOutLap ? 1 : 0);
            this.AttachDelegate("Booleans.IsInLap", () => this.Values.Booleans.NewData.IsInLap ? 1 : 0);
            this.AttachDelegate("Booleans.EcuMapChangedThisLap", () => this.Values.Booleans.NewData.EcuMapChangedThisLap ? 1 : 0);
            this.AttachDelegate("Booleans.RainIntensityChangedThisLap", () => this.Values.Booleans.NewData.RainIntensityChangedThisLap ? 1 : 0);
            //this.AttachDelegate("Booleans.IsBroadcastClientConnected", () => _values.RawData.BroadcastClient?.IsConnected ?? false ? 1 : 0);

            this.AttachDelegate("Fuel.Remaining", () => this.Values.Car.Fuel.Remaining);
            this.AttachDelegate("Fuel.RemainingAtLapStart", () => this.Values.Car.Fuel.RemainingAtLapStart);

            void addStats(string name, IStats values, StatsFlags settings) {
                if ((StatsFlags.Min & settings) != 0) {
                    this.AttachDelegate(name + "." + MIN_KEYWORD, () => values.Min);
                }
                if ((StatsFlags.Max & settings) != 0) {
                    this.AttachDelegate(name + "." + MAX_KEYWORD, () => values.Max);
                }
                if ((StatsFlags.Avg & settings) != 0) {
                    this.AttachDelegate(name + "." + AVG_KEYWORD, () => values.Avg);
                }
                if ((StatsFlags.Std & settings) != 0) {
                    this.AttachDelegate(name + "." + STD_KEYWORD, () => values.Std);
                }
                if ((StatsFlags.Median & settings) != 0) {
                    this.AttachDelegate(name + ".Median", () => values.Median);
                }
                if ((StatsFlags.Q1 & settings) != 0) {
                    this.AttachDelegate(name + ".Q1", () => values.Q1);
                }
                if ((StatsFlags.Q3 & settings) != 0) {
                    this.AttachDelegate(name + ".Q3", () => values.Q3);
                }
            }

            addStats("Laps.Time", this.Values.Laps.PrevTimes.Stats, Settings.PrevLapsStatsFlags);
            addStats("Laps.S1Time", this.Values.Laps.PrevS1Times.Stats, Settings.PrevLapsStatsFlags);
            addStats("Laps.S2Time", this.Values.Laps.PrevS2Times.Stats, Settings.PrevLapsStatsFlags);
            addStats("Laps.S3Time", this.Values.Laps.PrevS3Times.Stats, Settings.PrevLapsStatsFlags);
            addStats("Fuel.UsedPerLap", this.Values.Car.Fuel.PrevUsedPerLap.Stats, Settings.PrevFuelPerLapStatsFlags);
            addStats("Fuel.LapsRemaining", this.Values.RemainingOnFuel.Laps, Settings.RemainingStatsFlags);
            addStats("Fuel.TimeRemaining", this.Values.RemainingOnFuel.Time, Settings.RemainingStatsFlags);
            addStats("Session.LapsRemaining", this.Values.RemainingInSession.Laps, Settings.RemainingStatsFlags);
            addStats("Session.TimeRemaining", this.Values.RemainingInSession.Time, Settings.RemainingStatsFlags);
            addStats("Fuel.NeededInSession", this.Values.RemainingInSession.FuelNeeded, Settings.RemainingStatsFlags);


            void addTyres<T>(string name, IWheelsData<T> values) {
                this.AttachDelegate(name + "." + Car.Tyres.Names.FL, () => values.FL);
                this.AttachDelegate(name + "." + Car.Tyres.Names.FR, () => values.FR);
                this.AttachDelegate(name + "." + Car.Tyres.Names.RL, () => values.RL);
                this.AttachDelegate(name + "." + Car.Tyres.Names.RR, () => values.RR);
            }

            addTyres("Tyres.IdealInputPres", this.Values.Car.Tyres.IdealInputPres);
            addTyres("Tyres.PredictedIdealInputPresDry", this.Values.Car.Tyres.PredictedIdealInputPresDry);
            addTyres("Tyres.PredictedIdealInputPresWet", this.Values.Car.Tyres.PredictedIdealInputPresNowWet);
            addTyres("Tyres.PredictedIdealInputPresIn30MinWet", this.Values.Car.Tyres.PredictedIdealInputPresFutureWet);
            addTyres("Tyres.CurrentInputPres", this.Values.Car.Tyres.CurrentInputPres);
            addTyres("Tyres.PresLoss", this.Values.Car.Tyres.PresLoss);
            addTyres("Tyres.PresAvgDeltaToIdeal", this.Values.Car.Tyres.PressDeltaToIdeal);

            void addTyresNormalized<T>(string name, IWheelsData<T> values, WheelFlags flag) {
                if ((WheelFlags.Color & flag) != 0) {
                    this.AttachDelegate(name + "." + Car.Tyres.Names.FL + "." + NORMALIZED_KEYWORD, () => values.FL);
                    this.AttachDelegate(name + "." + Car.Tyres.Names.FR + "." + NORMALIZED_KEYWORD, () => values.FR);
                    this.AttachDelegate(name + "." + Car.Tyres.Names.RL + "." + NORMALIZED_KEYWORD, () => values.RL);
                    this.AttachDelegate(name + "." + Car.Tyres.Names.RR + "." + NORMALIZED_KEYWORD, () => values.RR);
                }
            }

            addTyresNormalized("Tyres.Pres", this.Values.Car.Tyres.PresNormalized, Settings.TyrePresFlags);
            addTyresNormalized("Tyres.Temp", this.Values.Car.Tyres.TempNormalized, Settings.TyreTempFlags);
            addTyresNormalized("Brakes.Temp", this.Values.Car.Brakes.TempNormalized, Settings.BrakeTempFlags);

            void addTyreStatsNormalized<T>(string name, IWheelsData<T> values, string statname) {
                this.AttachDelegate(name + "." + Car.Tyres.Names.FL + "." + statname + "." + NORMALIZED_KEYWORD, () => values.FL);
                this.AttachDelegate(name + "." + Car.Tyres.Names.FR + "." + statname + "." + NORMALIZED_KEYWORD, () => values.FR);
                this.AttachDelegate(name + "." + Car.Tyres.Names.RL + "." + statname + "." + NORMALIZED_KEYWORD, () => values.RL);
                this.AttachDelegate(name + "." + Car.Tyres.Names.RR + "." + statname + "." + NORMALIZED_KEYWORD, () => values.RR);
            }

            void addTyresStats<T, S>(
                string name,
                IWheelsData<S> values,
                IWheelsData<T> min,
                IWheelsData<T> avg,
                IWheelsData<T> max,
                WheelFlags flags
            ) where S : IStats {
                void _addStats(string n, IStats v) {
                    if ((WheelFlags.Min & flags) != 0) {
                        this.AttachDelegate(n + "." + MIN_KEYWORD, () => v.Min);
                    }
                    if ((WheelFlags.Max & flags) != 0) {
                        this.AttachDelegate(n + "." + MAX_KEYWORD, () => v.Max);
                    }
                    if ((WheelFlags.Avg & flags) != 0) {
                        this.AttachDelegate(n + "." + AVG_KEYWORD, () => v.Avg);
                    }
                    if ((WheelFlags.Std & flags) != 0) {
                        this.AttachDelegate(n + "." + STD_KEYWORD, () => v.Std);
                    }
                }
                _addStats(name + "." + Car.Tyres.Names.FL, values.FL);
                _addStats(name + "." + Car.Tyres.Names.FR, values.FR);
                _addStats(name + "." + Car.Tyres.Names.RL, values.RL);
                _addStats(name + "." + Car.Tyres.Names.RR, values.RR);

                if ((WheelFlags.MinColor & Settings.TyrePresFlags) != 0) {
                    addTyreStatsNormalized(name, min, "Min");
                }

                if ((WheelFlags.MaxColor & Settings.TyrePresFlags) != 0) {
                    addTyreStatsNormalized(name, max, "Max");
                }

                if ((WheelFlags.AvgColor & Settings.TyrePresFlags) != 0) {
                    addTyreStatsNormalized(name, avg, "Avg");
                }

            }

            addTyresStats(
                "Tyres.Pres",
                this.Values.Car.Tyres.PresOverLap,
                this.Values.Car.Tyres.PresMinNormalized,
                this.Values.Car.Tyres.PresAvgNormalized,
                this.Values.Car.Tyres.PresMaxNormalized,
                Settings.TyrePresFlags
            );
            addTyresStats(
                "Tyres.Temp",
                this.Values.Car.Tyres.TempOverLap,
                this.Values.Car.Tyres.TempMinNormalized,
                this.Values.Car.Tyres.TempAvgNormalized,
                this.Values.Car.Tyres.TempMaxNormalized,
                Settings.TyreTempFlags
            );
            addTyresStats(
                "Brakes.Temp",
                this.Values.Car.Brakes.TempOverLap,
                this.Values.Car.Brakes.TempMinNormalized,
                this.Values.Car.Brakes.TempAvgNormalized,
                this.Values.Car.Brakes.TempMaxNormalized,
                Settings.BrakeTempFlags
            );

            void addTyresStatsOnlyAvg<T, S>(string name, IWheelsData<S> values, IWheelsData<T> avg, WheelFlags flags) where S : IStats {
                void _addStats(string n, IStats v) {
                    if ((WheelFlags.Avg & flags) != 0) {
                        this.AttachDelegate(n + ".Avg", () => v.Avg);
                    }
                }
                _addStats(name + '.' + Car.Tyres.Names.FL, values.FL);
                _addStats(name + '.' + Car.Tyres.Names.FR, values.FR);
                _addStats(name + '.' + Car.Tyres.Names.RL, values.RL);
                _addStats(name + '.' + Car.Tyres.Names.RR, values.RR);

                if ((WheelFlags.AvgColor & Settings.TyrePresFlags) != 0) {
                    addTyreStatsNormalized(name, avg, "Avg");
                }
            }

            addTyresStatsOnlyAvg("Tyres.Temp.Inner", this.Values.Car.Tyres.TempInnerOverLap, this.Values.Car.Tyres.TempInnerAvgNormalized, Settings.TyreTempFlags);
            addTyresStatsOnlyAvg("Tyres.Temp.Middle", this.Values.Car.Tyres.TempMiddleOverLap, this.Values.Car.Tyres.TempMiddleAvgNormalized, Settings.TyreTempFlags);
            addTyresStatsOnlyAvg("Tyres.Temp.Outer", this.Values.Car.Tyres.TempOuterOverLap, this.Values.Car.Tyres.TempOuterAvgNormalized, Settings.TyreTempFlags);


            // this is a hacky but the only way this works is if the indices in `values[x]` are directly written in
            void addPrevData(string name, FixedSizeDequeStats values) {
#pragma warning disable IDE0011 // Add braces
                if (Settings.NumPreviousValuesStored > 0) this.AttachDelegate(name + ".0", () => values[0]);
                if (Settings.NumPreviousValuesStored > 1) this.AttachDelegate(name + ".1", () => values[1]);
                if (Settings.NumPreviousValuesStored > 2) this.AttachDelegate(name + ".2", () => values[2]);
                if (Settings.NumPreviousValuesStored > 3) this.AttachDelegate(name + ".3", () => values[3]);
                if (Settings.NumPreviousValuesStored > 4) this.AttachDelegate(name + ".4", () => values[4]);
                if (Settings.NumPreviousValuesStored > 5) this.AttachDelegate(name + ".5", () => values[5]);
                if (Settings.NumPreviousValuesStored > 6) this.AttachDelegate(name + ".6", () => values[6]);
                if (Settings.NumPreviousValuesStored > 7) this.AttachDelegate(name + ".7", () => values[7]);
                if (Settings.NumPreviousValuesStored > 8) this.AttachDelegate(name + ".8", () => values[8]);
                if (Settings.NumPreviousValuesStored > 9) this.AttachDelegate(name + ".9", () => values[9]);
                if (Settings.NumPreviousValuesStored > 10) this.AttachDelegate(name + ".10", () => values[10]);
                if (Settings.NumPreviousValuesStored > 11) this.AttachDelegate(name + ".11", () => values[11]);
                if (Settings.NumPreviousValuesStored > 12) this.AttachDelegate(name + ".12", () => values[12]);
                if (Settings.NumPreviousValuesStored > 13) this.AttachDelegate(name + ".13", () => values[13]);
                if (Settings.NumPreviousValuesStored > 14) this.AttachDelegate(name + ".14", () => values[14]);
                if (Settings.NumPreviousValuesStored > 15) this.AttachDelegate(name + ".15", () => values[15]);
                if (Settings.NumPreviousValuesStored > 16) this.AttachDelegate(name + ".16", () => values[16]);
                if (Settings.NumPreviousValuesStored > 17) this.AttachDelegate(name + ".17", () => values[17]);
                if (Settings.NumPreviousValuesStored > 18) this.AttachDelegate(name + ".18", () => values[18]);
                if (Settings.NumPreviousValuesStored > 19) this.AttachDelegate(name + ".19", () => values[19]);
                if (Settings.NumPreviousValuesStored > 20) this.AttachDelegate(name + ".20", () => values[20]);
                if (Settings.NumPreviousValuesStored > 21) this.AttachDelegate(name + ".21", () => values[21]);
                if (Settings.NumPreviousValuesStored > 22) this.AttachDelegate(name + ".22", () => values[22]);
                if (Settings.NumPreviousValuesStored > 23) this.AttachDelegate(name + ".23", () => values[23]);
                if (Settings.NumPreviousValuesStored > 24) this.AttachDelegate(name + ".24", () => values[24]);
                if (Settings.NumPreviousValuesStored > 25) this.AttachDelegate(name + ".25", () => values[25]);
                if (Settings.NumPreviousValuesStored > 26) this.AttachDelegate(name + ".26", () => values[26]);
                if (Settings.NumPreviousValuesStored > 27) this.AttachDelegate(name + ".27", () => values[27]);
                if (Settings.NumPreviousValuesStored > 28) this.AttachDelegate(name + ".28", () => values[28]);
                if (Settings.NumPreviousValuesStored > 29) this.AttachDelegate(name + ".29", () => values[29]);
                if (Settings.NumPreviousValuesStored > 30) this.AttachDelegate(name + ".30", () => values[30]);
#pragma warning restore IDE0011 // Add braces
            }

            addPrevData("Laps.PrevTime", this.Values.Laps.PrevTimes);
            addPrevData("Laps.PrevS1Time", this.Values.Laps.PrevS1Times);
            addPrevData("Laps.PrevS2Time", this.Values.Laps.PrevS2Times);
            addPrevData("Laps.PrevS3Time", this.Values.Laps.PrevS3Times);
            addPrevData("Fuel.PrevUsedPerLap", this.Values.Car.Fuel.PrevUsedPerLap);

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
                LogToFile($"{DateTime.Now:dd.MM.yyyy HH:mm.ss} INFO ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}\n");
            }
        }

        public static void LogWarn(string msq, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            var pathParts = sourceFilePath.Split('\\');
            SimHub.Logging.Current.Warn($"{PluginName} ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}");
            LogToFile($"{DateTime.Now:dd.MM.yyyy HH:mm.ss} WARN ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}\n");
        }

        public static void LogError(string msq, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            var pathParts = sourceFilePath.Split('\\');
            SimHub.Logging.Current.Error($"{PluginName} ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}");
            LogToFile($"{DateTime.Now:dd.MM.yyyy HH:mm.ss} ERROR ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}\n");
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
                    RuntimeHelpers.PrepareMethod(method.MethodHandle);
                }
            }

            var t = sw.Elapsed;
            LogInfo($"Prejit finished in {t.TotalMilliseconds}ms");

        }

    }


    public static class Helpers {
        public static string GetAccCarClass(string name) {
            name = name.ToLower();
            if (name.Contains("gt4")) {
                return "gt4";
            }

            return name switch {
                "porsche_991ii_gt3_cup"
                or "porsche_992_gt3_cup"
                or "ferrari_488_challenge_evo"
                or "lamborghini_huracan_st_evo2"
                or "lamborghini_huracan_st" => "gtc",

                "bmw_m2_cs_racing" => "tcx",
                _ => "gt3",
            };
        }
    }
}