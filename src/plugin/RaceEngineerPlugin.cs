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

        private const string SETTINGS_PATH = @"PluginsData\RaceEngineerPlugin\Settings.json";
        public static readonly Settings SETTINGS = ReadSettings();
        public static Game.Game GAME; // Const during the lifetime of this plugin, plugin is rebuilt at game change
        public static string GAME_PATH; // Same as above
        private static FileStream f;
        private static StreamWriter sw;

        private Values values;

        private static Settings ReadSettings() {
            if (File.Exists(SETTINGS_PATH)) {
                return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SETTINGS_PATH).Replace("\"", "'"));
            } else {
                var settings = new Settings();
                string txt = JsonConvert.SerializeObject(settings, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(SETTINGS_PATH));
                File.WriteAllText(SETTINGS_PATH, txt);
                return settings;
            }
        }


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
                    //Stopwatch stopWatch = new Stopwatch();
                    //stopWatch.Start();

                    values.OnDataUpdate(pluginManager, data);

                    #region UPDATE PROPERTIES

                    // Current value colors
                    pluginManager.SetPropertyValue(TYRE_PRES_PROP_NAME + tyreNames[0] + "Color", this.GetType(), values.car.Tyres.PresColorF.GetHexColor(data.NewData.TyrePressureFrontLeft));
                    pluginManager.SetPropertyValue(TYRE_PRES_PROP_NAME + tyreNames[1] + "Color", this.GetType(), values.car.Tyres.PresColorF.GetHexColor(data.NewData.TyrePressureFrontRight));
                    pluginManager.SetPropertyValue(TYRE_PRES_PROP_NAME + tyreNames[2] + "Color", this.GetType(), values.car.Tyres.PresColorR.GetHexColor(data.NewData.TyrePressureRearLeft));
                    pluginManager.SetPropertyValue(TYRE_PRES_PROP_NAME + tyreNames[3] + "Color", this.GetType(), values.car.Tyres.PresColorR.GetHexColor(data.NewData.TyrePressureRearRight));

                    pluginManager.SetPropertyValue(TYRE_TEMP_PROP_NAME + tyreNames[0] + "Color", this.GetType(), values.car.Tyres.TempColorF.GetHexColor(data.NewData.TyreTemperatureFrontLeft));
                    pluginManager.SetPropertyValue(TYRE_TEMP_PROP_NAME + tyreNames[1] + "Color", this.GetType(), values.car.Tyres.TempColorF.GetHexColor(data.NewData.TyreTemperatureFrontRight));
                    pluginManager.SetPropertyValue(TYRE_TEMP_PROP_NAME + tyreNames[2] + "Color", this.GetType(), values.car.Tyres.TempColorR.GetHexColor(data.NewData.TyreTemperatureRearLeft));
                    pluginManager.SetPropertyValue(TYRE_TEMP_PROP_NAME + tyreNames[3] + "Color", this.GetType(), values.car.Tyres.TempColorR.GetHexColor(data.NewData.TyreTemperatureRearRight));

                    pluginManager.SetPropertyValue(BRAKE_TEMP_PROP_NAME + tyreNames[0] + "Color", this.GetType(), values.car.Brakes.TempColor.GetHexColor(data.NewData.BrakeTemperatureFrontLeft));
                    pluginManager.SetPropertyValue(BRAKE_TEMP_PROP_NAME + tyreNames[1] + "Color", this.GetType(), values.car.Brakes.TempColor.GetHexColor(data.NewData.BrakeTemperatureFrontRight));
                    pluginManager.SetPropertyValue(BRAKE_TEMP_PROP_NAME + tyreNames[2] + "Color", this.GetType(), values.car.Brakes.TempColor.GetHexColor(data.NewData.BrakeTemperatureRearLeft));
                    pluginManager.SetPropertyValue(BRAKE_TEMP_PROP_NAME + tyreNames[3] + "Color", this.GetType(), values.car.Brakes.TempColor.GetHexColor(data.NewData.BrakeTemperatureRearRight));

                    for (int i = 0; i < values.car.Fuel.PrevUsedPerLap.Count; i++) {
                        double lapTime = values.laps.PrevTimes[i];
                        double lapDiffPercent = lapTime / values.laps.PrevTimes.Avg * 100 - 100;
                        var istr = i.ToString();

                        pluginManager.SetPropertyValue(PREV_LAPTIME_PROP_NAME + istr, this.GetType(), fromSeconds(lapTime));
                        pluginManager.SetPropertyValue(PREV_LAPTIME_PROP_NAME + "Sec" + istr, this.GetType(), lapTime);
                        pluginManager.SetPropertyValue(PREV_LAPTIME_PROP_NAME + "DeltaToAvg" + (i).ToString(), this.GetType(), lapTime - values.laps.PrevTimes.Avg);

                        pluginManager.SetPropertyValue(PREV_FUEL_PROP_NAME + istr, this.GetType(), values.car.Fuel.PrevUsedPerLap[i]);
                    }
                    #endregion

                    //stopWatch.Stop();
                    //TimeSpan ts = stopWatch.Elapsed;
                    //File.AppendAllText("Logs/RETiming.txt", $"{ts.TotalMilliseconds}\n");
                }
            } else {
                values.booleans.OnGameNotRunning();
                values.db.CommitTransaction();
            }
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager) {
            // Save settings
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
                var fpath = $"{SETTINGS.DataLocation}\\Logs\\RELog_{DateTime.Now.Ticks}.txt";
                Directory.CreateDirectory(Path.GetDirectoryName(fpath));
                f = File.Create(fpath);
                sw = new StreamWriter(f);
            }

            LogInfo("Starting plugin");
            Settings = this.ReadCommonSettings<RaceEngineerPluginSettings>("GeneralSettings", () => new RaceEngineerPluginSettings());

            // DataCorePlugin should be built before, thus this property should be available.
            var gameName = (string)pluginManager.GetPropertyValue("DataCorePlugin.CurrentGame");
            GAME = new Game.Game(gameName);
            GAME_PATH = $@"{SETTINGS.DataLocation}\{gameName}";
            values = new Values();

            #region ADD DELEGATES

            this.AttachDelegate("FuelLeft", () => values.car.Fuel.Remaining);
            this.AttachDelegate("IsOnTrack", () => values.booleans.NewData.IsOnTrack);
            this.AttachDelegate("IsValidFuelLap", () => values.booleans.NewData.IsValidFuelLap);

            Action<string, Stats.Stats, bool> addStats = (name, values, include_std) => {
                this.AttachDelegate(name + Stats.Stats.names[0], () => values[0]);
                this.AttachDelegate(name + Stats.Stats.names[1], () => values[1]);
                this.AttachDelegate(name + Stats.Stats.names[2], () => values[2]);
                if (include_std) {
                    this.AttachDelegate(name + Stats.Stats.names[3], () => values[3]);
                }
            };

            Action<string, Stats.Stats, bool> addStatsTimespan = (name, values, include_std) => {
                this.AttachDelegate(name + Stats.Stats.names[0], () => fromSeconds(values[0]));
                this.AttachDelegate(name + Stats.Stats.names[1], () => fromSeconds(values[1]));
                this.AttachDelegate(name + Stats.Stats.names[2], () => fromSeconds(values[2]));
                if (include_std) {
                    this.AttachDelegate(name + Stats.Stats.names[3], () => fromSeconds(values[3]));
                }
            };

            addStats("LapsRemainingOnFuel", values.remainingOnFuel.laps, false);
            addStats("TimeRemainingOnFuel", values.remainingOnFuel.time, false);
            addStats("LapsRemainingInSession", values.remainingInSession.laps, false);
            addStats("TimeRemainingInSession", values.remainingInSession.time, false);
            addStats("FuelNeededInSession", values.remainingInSession.fuelNeeded, false);
            addStats("FuelPerLap", values.car.Fuel.PrevUsedPerLap.Stats, true);
            addStats("LapTimeSec", values.laps.PrevTimes.Stats, true);
            addStatsTimespan("LapTime", values.laps.PrevTimes.Stats, true);

            Action<string, double[]> addTyres = (name, values) => {
                this.AttachDelegate(name + tyreNames[0], () => values[0]);
                this.AttachDelegate(name + tyreNames[1], () => values[1]);
                this.AttachDelegate(name + tyreNames[2], () => values[2]);
                this.AttachDelegate(name + tyreNames[3], () => values[3]);
            };

            Action<string, Stats.Stats, Color.ColorCalculator> addStatsWColor = (name, values, cc) => {
                this.AttachDelegate(name + Stats.Stats.names[0], () => values[0]);
                this.AttachDelegate(name + Stats.Stats.names[0] + "Color", () => cc.GetHexColor(values[0]));

                this.AttachDelegate(name + Stats.Stats.names[1], () => values[1]);
                this.AttachDelegate(name + Stats.Stats.names[1] + "Color", () => cc.GetHexColor(values[1]));

                this.AttachDelegate(name + Stats.Stats.names[2], () => values[2]);
                this.AttachDelegate(name + Stats.Stats.names[2] + "Color", () => cc.GetHexColor(values[2]));

            };

            Action<string, Stats.WheelsStats, Color.ColorCalculator, Color.ColorCalculator> addTyresStats = (name, values, ccf, ccr) => {
                addStatsWColor(name + tyreNames[0], values[0], ccf);
                addStatsWColor(name + tyreNames[1], values[1], ccf);
                addStatsWColor(name + tyreNames[2], values[2], ccr);
                addStatsWColor(name + tyreNames[3], values[3], ccr);
            };

           
            addTyres("IdealInputTyrePres", values.car.Tyres.IdealInputPres);
            addTyres("CurrentInputTyrePres", values.car.Tyres.CurrentInputPres);
            addTyres("TyrePresLoss", values.car.Tyres.PresLoss);
            addTyresStats("TyrePresOverLap", values.car.Tyres.PresOverLap, values.car.Tyres.PresColorF, values.car.Tyres.PresColorR);
            addTyresStats("TyreTempOverLap", values.car.Tyres.TempOverLap, values.car.Tyres.TempColorF, values.car.Tyres.TempColorR);
            addTyresStats("BrakeTempOverLap", values.car.Brakes.TempOverLap, values.car.Brakes.TempColor, values.car.Brakes.TempColor);
            #endregion

            #region ADD PROPERTIES
            // Add some properties where we cannot use delegates

            // Need access to GameData which is not available here
            Action<string, object> addPropTyres = (name, value) => {
                pluginManager.AddProperty(name + tyreNames[0] + "Color", this.GetType(), value);
                pluginManager.AddProperty(name + tyreNames[1] + "Color", this.GetType(), value);
                pluginManager.AddProperty(name + tyreNames[2] + "Color", this.GetType(), value);
                pluginManager.AddProperty(name + tyreNames[3] + "Color", this.GetType(), value);
            };

            addPropTyres(TYRE_PRES_PROP_NAME, "#000000");
            addPropTyres(TYRE_TEMP_PROP_NAME, "#000000");
            addPropTyres(BRAKE_TEMP_PROP_NAME, "#000000");


            // Number of available properties depends on runtime conditions (eg how many laps are completed)
            for (int i = 0; i < SETTINGS.NumPreviousValuesStored; i++) {
                var istr = i.ToString();

                pluginManager.AddProperty(PREV_LAPTIME_PROP_NAME + istr, this.GetType(), fromSeconds(0.0));
                pluginManager.AddProperty(PREV_LAPTIME_PROP_NAME + "Sec" + istr, this.GetType(), 0.0);
                pluginManager.AddProperty(PREV_LAPTIME_PROP_NAME + "DeltaToAvg" + istr, this.GetType(), 0.0);
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
            if (SETTINGS.Log && f != null) { 
                sw.WriteLine(msq);
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
                LogToFile($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss")} ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}");
            }
        }

        public static void LogFileSeparator() {
            if (SETTINGS.Log) {
                LogToFile("\n----------------------------------------------------------\n");
            }
        }


    }
}