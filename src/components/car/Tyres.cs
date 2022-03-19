using GameReaderCommon;
using SimHub.Plugins;
using System;
using RaceEngineerPlugin.Stats;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace RaceEngineerPlugin.Car {

    public class Tyres {
        public string Name { get; private set; }

        public double[] IdealInputPres { get; }
        public double[] PredictedIdealInputPresDry { get; }
        public double[] PredictedIdealInputPresNowWet { get; }
        public double[] PredictedIdealInputPresFutureWet { get; }
        public double[] CurrentInputPres { get; }
        public double[] PresLoss { get; }
        public bool[] PresLossLap { get; }
        public string[] PresColor { get; private set; }
        public string[] TempColor { get; private set; }

        public WheelsStats PresOverLap { get; }
        public WheelsStats TempOverLap { get; }

        public Color.ColorCalculator PresColorF { get; private set; }
        public Color.ColorCalculator PresColorR { get; private set; }
        public Color.ColorCalculator TempColorF { get; private set; }
        public Color.ColorCalculator TempColorR { get; private set; }

        public Dictionary<string, Dictionary<int, int>> SetLaps { get; private set; }

        public InputTyrePresPredictor InputTyrePresPredictorDry { get; private set; }
        public InputTyrePresPredictor InputTyrePresPredictorNowWet { get; private set; }
        public InputTyrePresPredictor InputTyrePresPredictorFutureWet { get; private set; }

        private volatile bool updatingPresPredictorDry = false;
        private volatile bool updatingPresPredictorNowWet = false;
        private volatile bool updatingPresPredictorFutureWet = false;
        private WheelsRunningStats presRunning = new WheelsRunningStats();
        private WheelsRunningStats tempRunning = new WheelsRunningStats();
        private TyreInfo tyreInfo = null;
        private double lastSampleTimeSec = DateTime.Now.Second;
        private int wetSet = 0;
        public int currentTyreSet = 0;

        public Tyres() {
            RaceEngineerPlugin.LogInfo("Created new Tyres");
            PresOverLap = new WheelsStats();
            TempOverLap = new WheelsStats();
            IdealInputPres = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN};
            PredictedIdealInputPresDry = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN };
            PredictedIdealInputPresNowWet = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN };
            PredictedIdealInputPresFutureWet = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN };
            CurrentInputPres = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN };
            PresLoss = new double[4] { 0.0, 0.0, 0.0, 0.0 };
            PresLossLap = new bool[4] { false, false, false, false };
            SetLaps = new Dictionary<string, Dictionary<int, int>>();
            PresColor = new string[4] { "#000000", "#000000", "#000000", "#000000" };
            TempColor = new string[4] { "#000000", "#000000", "#000000", "#000000" };
            Reset();
        }

        public void Reset() {
            Name = null;

            for (int i = 0; i < 4; i++) {
                IdealInputPres[i] = double.NaN;
                PredictedIdealInputPresDry[i] = double.NaN;
                PredictedIdealInputPresNowWet[i] = double.NaN;
                PredictedIdealInputPresFutureWet[i] = double.NaN;
                CurrentInputPres[i] = double.NaN;
                PresLoss[i] = 0.0;
                PresLossLap[i] = false;
                PresColor[i] = "#000000";
                TempColor[i] = "#000000";
            }

            PresOverLap.Reset();
            TempOverLap.Reset();

            PresColorF = new Color.ColorCalculator(RaceEngineerPlugin.SETTINGS.PresColor, RaceEngineerPlugin.SETTINGS.TyrePresColorDefValues);
            PresColorR = new Color.ColorCalculator(RaceEngineerPlugin.SETTINGS.PresColor, RaceEngineerPlugin.SETTINGS.TyrePresColorDefValues);
            TempColorF = new Color.ColorCalculator(RaceEngineerPlugin.SETTINGS.TempColor, RaceEngineerPlugin.SETTINGS.TyreTempColorDefValues);
            TempColorR = new Color.ColorCalculator(RaceEngineerPlugin.SETTINGS.TempColor, RaceEngineerPlugin.SETTINGS.TyreTempColorDefValues);

            SetLaps.Clear();
            InputTyrePresPredictorDry = null;
            InputTyrePresPredictorNowWet = null;
            InputTyrePresPredictorFutureWet = null;

            updatingPresPredictorDry = false;
            updatingPresPredictorNowWet = false;
            updatingPresPredictorFutureWet = false;
            presRunning.Reset();
            tempRunning.Reset();
            tyreInfo = null;
            wetSet = 0;
            currentTyreSet = 0;
        }

        public static string GetTyreCompound(PluginManager pluginManager) {
            return (RaceEngineerPlugin.GAME.IsAC || RaceEngineerPlugin.GAME.IsACC) ? (string)pluginManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.TyreCompound") : null;
        }

        public int GetCurrentSetLaps() {
            return SetLaps[Name][currentTyreSet];
        }

        #region On... METHODS

        public void OnNewStint(PluginManager pluginManager, Database.Database db) {
            if (RaceEngineerPlugin.GAME.IsACC) {
                if (!SetLaps[Name].ContainsKey(currentTyreSet)) {
                    SetLaps[Name][currentTyreSet] = 0;
                }
            }
        }

        public void OnLapFinished(PluginManager pm, double airtemp, double tracktemp) {
            if (RaceEngineerPlugin.GAME.IsACC) {
                SetLaps[Name][currentTyreSet] += 1;
            }

            PresOverLap.Update(presRunning);
            TempOverLap.Update(tempRunning);
            UpdateIdealInputPressures(airtemp, tracktemp);
            presRunning.Reset();
            tempRunning.Reset();
        }

        public void OnLapFinishedAfterInsert() {
            for (int i = 0; i < 4; i++) {
                PresLossLap[i] = false;
            }
        }

        public void OnRegularUpdate(PluginManager pm, GameData data, Values v) {
            //Stopwatch sw2 = Stopwatch.StartNew();
            //Stopwatch sw = Stopwatch.StartNew();
            CheckCompoundChange(pm, data, v, data.NewData.TrackId);
            //sw.Stop();
            //sw2.Stop();
            //var ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\timings\\RETiming_Tyres_CheckCompoundChange_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //sw2.Start();
            //sw.Restart();
            CheckPresChange(data, v.booleans);
            //sw.Stop();
            //sw2.Stop();
            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\timings\\RETiming_Tyres_CheckPresChange_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //sw2.Start();
            //sw.Restart();
            UpdateOverLapData(data, v.booleans);
            //sw.Stop();
            //sw2.Stop();
            // ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\timings\\RETiming_Tyres_UpdateOverLap_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //sw2.Start();
            //sw.Restart();
            PredictIdealInputPressures(pm, v);
            //sw.Stop();
            //sw2.Stop();
            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\timings\\RETiming_Tyres_PredInputPres_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");


            //sw2.Start();
            //sw.Restart();
            if (!v.booleans.NewData.IsInMenu) {
                if ((WheelFlags.Color & RaceEngineerPlugin.SETTINGS.TyrePresFlags) != 0) {
                    PresColor[0] = PresColorF.GetColor(data.NewData.TyrePressureFrontLeft).ToHEX();
                    PresColor[1] = PresColorF.GetColor(data.NewData.TyrePressureFrontRight).ToHEX();
                    PresColor[2] = PresColorR.GetColor(data.NewData.TyrePressureRearLeft).ToHEX();
                    PresColor[3] = PresColorR.GetColor(data.NewData.TyrePressureRearRight).ToHEX();
                }

                if ((WheelFlags.Color & RaceEngineerPlugin.SETTINGS.TyreTempFlags) != 0) {
                    TempColor[0] = TempColorF.GetColor(data.NewData.TyreTemperatureFrontLeft).ToHEX();
                    TempColor[1] = TempColorF.GetColor(data.NewData.TyreTemperatureFrontRight).ToHEX();
                    TempColor[2] = TempColorR.GetColor(data.NewData.TyreTemperatureRearLeft).ToHEX();
                    TempColor[3] = TempColorR.GetColor(data.NewData.TyreTemperatureRearRight).ToHEX();
                }
            }

            //sw.Stop();
            //sw2.Stop();
            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\timings\\RETiming_Tyres_Color_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //ts = sw2.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\timings\\RETiming_Tyres_total_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");
        }

        #endregion

        #region PRIVATE METHODS

        private void CheckCompoundChange(PluginManager pm, GameData data, Values v, string trackName) {
            // Pads can change at two moments:
            //    a) If we exit garage
            //    b) If we change tyres in pit stop.

            if (Name != null && !(v.booleans.NewData.ExitedMenu || v.booleans.NewData.ExitedPitBox)) return;

            string newTyreName = GetTyreCompound(pm);

            if (newTyreName == "wet_compound") {
                if (v.booleans.NewData.ExitedMenu) {
                    wetSet += 1; // Definitely is new set
                } else if (v.booleans.NewData.ExitedPitBox) {
                    // Could be new set but there is really no way to tell since wet sets are not numbered
                    // Since tyre change takes 30 seconds, let's assume that if pitstop is 29s or longer that we changed tyres. 
                    // Not 100% true since with repairs or brake change we can get longer pitstops
                    // But we do know that if pit was shorter than 30s, we couldn't have changed tyres.
                    RaceEngineerPlugin.LogInfo($"Exited pit box: pit time = {data.OldData.IsInPitSince}");
                    if (data.OldData.IsInPitSince > 29) {
                        wetSet += 1;
                    }
                }
                currentTyreSet = wetSet;
            } else {
                currentTyreSet = RaceEngineerPlugin.GAME.IsACC ? (int)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.currentTyreSet") : -1;
            }

            if (newTyreName == null || newTyreName == Name) return;
            RaceEngineerPlugin.LogInfo($"Tyres changed from '{Name}' to '{newTyreName}'.");
            ResetValues();

            Name = newTyreName;
            if (!SetLaps.ContainsKey(Name)) {
                SetLaps[Name] = new Dictionary<int, int>();
            }

            if (v.car?.Info?.Tyres != null) {
                tyreInfo = v.car.Info.Tyres?[Name];
                if (tyreInfo != null) {
                    PresColorF.UpdateInterpolation(tyreInfo.IdealPres.F, tyreInfo.IdealPresRange.F);
                    PresColorR.UpdateInterpolation(tyreInfo.IdealPres.R, tyreInfo.IdealPresRange.R);
                    TempColorF.UpdateInterpolation(tyreInfo.IdealTemp.F, tyreInfo.IdealTempRange.F);
                    TempColorR.UpdateInterpolation(tyreInfo.IdealTemp.R, tyreInfo.IdealTempRange.R);
                } else {
                    ResetColors();
                }

                InitInputTyrePresPredictorDry(trackName, v.car.Name, v.car.Setup.advancedSetup.aeroBalance.brakeDuct, v.weather, v.db);
                if (v.weather.RainIntensity != ACCEnums.RainIntensity.NoRain) {
                    InitInputTyrePresPredictorNowWet(trackName, v.car.Name, v.car.Setup.advancedSetup.aeroBalance.brakeDuct, v.weather, RaceEngineerPlugin.TrackGripStatus(pm), v.db);
                }
                if (v.weather.RainIntensityIn30Min != ACCEnums.RainIntensity.NoRain) {
                    InitInputTyrePresPredictorFutureWet(trackName, v.car.Name, v.car.Setup.advancedSetup.aeroBalance.brakeDuct, v.weather, v.db);
                }

            } else {
                RaceEngineerPlugin.LogInfo($"Current CarInfo '{v.car.Name}' doesn't have specs for tyres. Resetting to defaults.");
            }
        }

        private void ResetPressureLoss() {
            for (int i = 0; i < 4; i++) {
                PresLoss[i] = 0.0;
            }
        }

        private const double PRESS_LOSS_THRESHOLD = 0.1; 
        private void CheckPresChange(GameData data, Booleans.Booleans booleans) {
            if (booleans.NewData.IsInMenu) {
                return;
            }

            var presDelta = new double[4] {
                 data.NewData.TyrePressureFrontLeft - data.OldData.TyrePressureFrontLeft,
                 data.NewData.TyrePressureFrontRight - data.OldData.TyrePressureFrontRight,
                 data.NewData.TyrePressureRearLeft - data.OldData.TyrePressureRearLeft,
                 data.NewData.TyrePressureRearRight - data.OldData.TyrePressureRearRight
            };

            if (data.NewData.SpeedKmh < 10) {
                Func<double, bool> pred = v => Math.Abs(v) > PRESS_LOSS_THRESHOLD;
                if (pred(presDelta[0]) || pred(presDelta[1]) || pred(presDelta[2]) || pred(presDelta[3])) {
                    RaceEngineerPlugin.LogInfo("Current input tyre pressures updated.");
                    CurrentInputPres[0] = Math.Ceiling(data.NewData.TyrePressureFrontLeft * 10.0) / 10.0;
                    CurrentInputPres[1] = Math.Ceiling(data.NewData.TyrePressureFrontRight * 10.0) / 10.0;
                    CurrentInputPres[2] = Math.Ceiling(data.NewData.TyrePressureRearLeft * 10.0) / 10.0;
                    CurrentInputPres[3] = Math.Ceiling(data.NewData.TyrePressureRearRight * 10.0) / 10.0;

                    ResetPressureLoss();

                }
            } else {
                for (int i = 0; i < 4; i++) {
                    if (presDelta[i] < -PRESS_LOSS_THRESHOLD) { 
                        PresLoss[i] += presDelta[i];
                        PresLossLap[i] = true;
                        RaceEngineerPlugin.LogInfo($"Pressure loss on {i} by {presDelta[i]}.");
                    }
                }
            }
        }

        private void UpdateOverLapData(GameData data, Booleans.Booleans booleans) {
            // Add sample to counters
            if (booleans.NewData.IsMoving && booleans.NewData.IsOnTrack) {
                double now = data.FrameTime.Second;
                if (lastSampleTimeSec == now) return;
                double[] currentPres = new double[] {
                    data.NewData.TyrePressureFrontLeft,
                    data.NewData.TyrePressureFrontRight,
                    data.NewData.TyrePressureRearLeft,
                    data.NewData.TyrePressureRearRight
                };
                double[] currentTemp = new double[] {
                    data.NewData.TyreTemperatureFrontLeft,
                    data.NewData.TyreTemperatureFrontRight,
                    data.NewData.TyreTemperatureRearLeft,
                    data.NewData.TyreTemperatureRearRight
                };

                presRunning.Update(currentPres);
                tempRunning.Update(currentTemp);

                lastSampleTimeSec = now;
            }
        }

        private void UpdateIdealInputPressures(double airtemp, double tracktemp) {
            if (tyreInfo != null) {    
                for (int i = 0; i < 4; i++) {
                    IdealInputPres[i] = CurrentInputPres[i] + (tyreInfo.IdealPres[i] - PresOverLap[i].Avg);
                }
            } else {
                RaceEngineerPlugin.LogInfo($"Couldn't update ideal tyre pressures as 'tyreInfo == null'");
            }
        }

        private void PredictIdealInputPressures(PluginManager pm, Values v) {
            if (tyreInfo == null || v.weather.AirTemp == 0.0) {
                RaceEngineerPlugin.LogInfo($"Couldn't update ideal tyre pressures as 'tyreInfo == null' || 'AirTemp == 0.0'");
                for (int i = 0; i < 4; i++) {
                    PredictedIdealInputPresDry[i] = double.NaN;
                    PredictedIdealInputPresNowWet[i] = double.NaN;
                    PredictedIdealInputPresFutureWet[i] = double.NaN;
                }
                return;
            }

            if (!updatingPresPredictorDry && (v.weather.RainIntensity == ACCEnums.RainIntensity.NoRain || v.weather.RainIntensityIn30Min == ACCEnums.RainIntensity.NoRain)) {
                if (InputTyrePresPredictorDry != null) {
                    var preds = InputTyrePresPredictorDry.Predict(v.weather.AirTemp, v.weather.TrackTemp, tyreInfo.IdealPres.F, tyreInfo.IdealPres.R);
                    preds.CopyTo(PredictedIdealInputPresDry, 0);
                } else {
                    InitInputTyrePresPredictorDry(v.track.Name, v.car.Name, v.car.Setup.advancedSetup.aeroBalance.brakeDuct, v.weather, v.db);
                    for (int i = 0; i < 4; i++) {
                        PredictedIdealInputPresDry[i] = double.NaN;
                    }
                }
            }

            if (!updatingPresPredictorNowWet && v.weather.RainIntensity != ACCEnums.RainIntensity.NoRain) {
                if (v.weather.RainIntensity != v.weather.PrevRainIntensity || InputTyrePresPredictorNowWet == null) {
                    InitInputTyrePresPredictorNowWet(v.track.Name, v.car.Name, v.car.Setup.advancedSetup.aeroBalance.brakeDuct, v.weather, RaceEngineerPlugin.TrackGripStatus(pm), v.db);
                    for (int i = 0; i < 4; i++) {
                        PredictedIdealInputPresNowWet[i] = double.NaN;
                    }
                } else {
                    var preds = InputTyrePresPredictorNowWet.Predict(v.weather.AirTemp, v.weather.TrackTemp, tyreInfo.IdealPres.F, tyreInfo.IdealPres.R);
                    preds.CopyTo(PredictedIdealInputPresNowWet, 0);
                }
            }

            if (!updatingPresPredictorFutureWet && v.weather.RainIntensityIn30Min != ACCEnums.RainIntensity.NoRain) {
                if (v.weather.RainIntensityIn30Min != v.weather.PrevRainIntensityIn30Min || InputTyrePresPredictorFutureWet == null) {
                    InitInputTyrePresPredictorFutureWet(v.track.Name, v.car.Name, v.car.Setup.advancedSetup.aeroBalance.brakeDuct, v.weather, v.db);
                    for (int i = 0; i < 4; i++) {
                        PredictedIdealInputPresFutureWet[i] = double.NaN;
                    }
                } else {
                    var preds = InputTyrePresPredictorNowWet.Predict(v.weather.AirTemp, v.weather.TrackTemp, tyreInfo.IdealPres.F, tyreInfo.IdealPres.R);
                    preds.CopyTo(PredictedIdealInputPresFutureWet, 0);
                }
            }
        }


        private void InitInputTyrePresPredictorDry(string trackName, string carName, int[] brakeDucts, Weather w, Database.Database db) {
            if (updatingPresPredictorDry) {
                RaceEngineerPlugin.LogWarn("Already updating dry tyre pres models. Shouldn't be possible. BUG SOMEWHERE.");
                return;
            };

            InputTyrePresPredictorDry = null;
            _ = Task.Run(() => {
                updatingPresPredictorDry = true;
                InputTyrePresPredictorDry = new InputTyrePresPredictor(trackName, carName, brakeDucts, "dry_compound", ACCEnums.RainIntensity.NoRain, $"(0, 1, 2)", db);
                updatingPresPredictorDry = false;
            });
            RaceEngineerPlugin.LogInfo("Started building dry tyre pres models.");
        }

        private void InitInputTyrePresPredictorNowWet(string trackName, string carName, int[] brakeDucts, Weather w, ACCEnums.TrackGrip trackGrip, Database.Database db) {
            if (updatingPresPredictorNowWet) {
                RaceEngineerPlugin.LogWarn("Already updating now wet tyre pres models. Shouldn't be possible. BUG SOMEWHERE.");
                return;
            }

            InputTyrePresPredictorNowWet = null;
            _ = Task.Run(() => {
                updatingPresPredictorNowWet = true;
                InputTyrePresPredictorNowWet = new InputTyrePresPredictor(trackName, carName, brakeDucts, "wet_compound", w.RainIntensityIn30Min, $"({(int)trackGrip})", db);
                updatingPresPredictorNowWet = false;
            });
            RaceEngineerPlugin.LogInfo("Started building now wet tyre pres models.");
        }

        private void InitInputTyrePresPredictorFutureWet(string trackName, string carName, int[] brakeDucts, Weather w, Database.Database db) {
            if (updatingPresPredictorFutureWet) {
                RaceEngineerPlugin.LogWarn("Already updating future wet tyre pres models. Shouldn't be possible. BUG SOMEWHERE.");
                return;
            }

            InputTyrePresPredictorFutureWet = null;
            _ = Task.Run(() => {
                updatingPresPredictorFutureWet = true;

                var futureTrackGrip = ACCEnums.TrackGrip.Wet;
                if (w.RainIntensityIn30Min == ACCEnums.RainIntensity.Thunderstorm) {
                    futureTrackGrip = ACCEnums.TrackGrip.Flooded;
                } else if (w.RainIntensityIn30Min == ACCEnums.RainIntensity.Drizzle
                    && (w.RainIntensity == ACCEnums.RainIntensity.NoRain || w.RainIntensity == ACCEnums.RainIntensity.Drizzle || w.RainIntensity == ACCEnums.RainIntensity.Light)
                ) {
                    futureTrackGrip = ACCEnums.TrackGrip.Damp;
                }

                InputTyrePresPredictorFutureWet = new InputTyrePresPredictor(trackName, carName, brakeDucts, "wet_compound", w.RainIntensityIn30Min, $"({(int)futureTrackGrip})", db);
                updatingPresPredictorFutureWet = false;
            });
            RaceEngineerPlugin.LogInfo("Started building future wet tyre pres models.");
        }


        private void ResetColors() {
            RaceEngineerPlugin.LogInfo("Tyres.ResetColors()");
            PresColorF = new Color.ColorCalculator(RaceEngineerPlugin.SETTINGS.PresColor, RaceEngineerPlugin.SETTINGS.TyrePresColorDefValues);
            PresColorR = new Color.ColorCalculator(RaceEngineerPlugin.SETTINGS.PresColor, RaceEngineerPlugin.SETTINGS.TyrePresColorDefValues);
            TempColorF = new Color.ColorCalculator(RaceEngineerPlugin.SETTINGS.TempColor, RaceEngineerPlugin.SETTINGS.TyreTempColorDefValues);
            TempColorR = new Color.ColorCalculator(RaceEngineerPlugin.SETTINGS.TempColor, RaceEngineerPlugin.SETTINGS.TyreTempColorDefValues);
        }

        private void ResetValues() {
            RaceEngineerPlugin.LogInfo("Tyres.ResetValues()");
            for (var i = 0; i < 4; i++) { 
                IdealInputPres[i] = double.NaN;
            }
            PresOverLap.Reset();
            TempOverLap.Reset();
            presRunning.Reset();
            tempRunning.Reset();
            InputTyrePresPredictorDry = null;
            InputTyrePresPredictorFutureWet = null;
            InputTyrePresPredictorNowWet = null;
            //tyreInfo = null;
        }

        #endregion

    }


    public class InputTyrePresPredictor {
        private ML.RidgeRegression[] regressors;
        private string trackName;
        private string carName;
        private int[] brakeDucts;
        private string compound;

        public InputTyrePresPredictor(string trackName, string carName, int[] brakeDucts, string compound, ACCEnums.RainIntensity rain_intensity, string trackGrip, Database.Database db) { 
            this.trackName = trackName;
            this.carName = carName;
            this.brakeDucts = brakeDucts;
            this.compound = compound;

            regressors = new ML.RidgeRegression[] {
                InitRegressor(0, rain_intensity, trackGrip, db), 
                InitRegressor(1, rain_intensity, trackGrip, db), 
                InitRegressor(2, rain_intensity, trackGrip, db), 
                InitRegressor(3, rain_intensity, trackGrip, db)
            };

            RaceEngineerPlugin.LogInfo($"Created InputTyrePresPredictor({trackName}, {carName}, [{brakeDucts[0]}, {brakeDucts[1]}], {compound})");
        }

        private ML.RidgeRegression InitRegressor(int tyre, ACCEnums.RainIntensity rainIntensity, string trackGrip, Database.Database db) {
            var data = db.GetInputPresData(tyre, carName, trackName, tyre < 3 ? brakeDucts[0] : brakeDucts[1], compound, trackGrip, rainIntensity);
            if (data.Item2.Count != 0) {
                return new ML.RidgeRegression(data.Item1, data.Item2);
            } else {
                return null;
            }            
        }

        public double[] Predict(double airtemp, double tracktemp, double idealPresFront, double idealPresRear) {
            var res = new double[4];
            for (int i = 0; i < 4; i++) {
                if (regressors[i] != null && airtemp != 0.0) {
                    res[i] = regressors[i].Predict(new double[] { i < 3 ? idealPresFront : idealPresRear, airtemp, tracktemp });
                } else { 
                    res[i] = double.NaN;
                }
            }
            return res;
        }

    }

}