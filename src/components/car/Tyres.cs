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
        public double[] PredictedIdealInputPres { get; }
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

        public InputTyrePresPredictor InputTyrePresPredictor { get; private set; }

        private volatile bool updatingPresPredictor = false;
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
            PredictedIdealInputPres = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN };
            CurrentInputPres = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN };
            PresLoss = new double[4] { 0.0, 0.0, 0.0, 0.0 };
            PresLossLap = new bool[4] { false, false, false, false };
            SetLaps = new Dictionary<string, Dictionary<int, int>>();
            PresColor = new string[4] { "#000000", "#000000", "#000000", "#000000" };
            TempColor = new string[4] { "#000000", "#000000", "#000000", "#000000" };
            ResetColors();
        }

        public void Reset() {
            Name = null;

            for (int i = 0; i < 4; i++) {
                IdealInputPres[i] = double.NaN;
                PredictedIdealInputPres[i] = double.NaN;
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
            InputTyrePresPredictor = null;


            updatingPresPredictor = false;
            presRunning.Reset();
            tempRunning.Reset();
            tyreInfo = null;
            wetSet = 0;
            currentTyreSet = 0;
        }

        public static string GetTyreCompound(PluginManager pluginManager) {
            return (RaceEngineerPlugin.GAME.IsAC || RaceEngineerPlugin.GAME.IsACC) ? (string)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.TyreCompound") : null;
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
            CheckCompoundChange(pm, data, v.car, data.NewData.TrackId, v.booleans, v.db);
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
            if (data.NewData.AirTemperature != 0.0) {
                PredictIdealInputPressures(data.NewData.AirTemperature, data.NewData.RoadTemperature, v.booleans);
            } else if (v.realtimeUpdate != null && double.IsNaN(PredictedIdealInputPres[0])) {
                PredictIdealInputPressures(v.realtimeUpdate.AmbientTemp, v.realtimeUpdate.TrackTemp, v.booleans);
            }
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

        private void CheckCompoundChange(PluginManager pm, GameData data, Car car, string trackName, Booleans.Booleans booleans, Database.Database db) {
            // Pads can change at two moments:
            //    a) If we exit garage
            //    b) If we change tyres in pit stop.

            if (Name != null && !(booleans.NewData.ExitedMenu || booleans.NewData.ExitedPitBox)) return;

            string newTyreName = GetTyreCompound(pm);

            if (newTyreName == "wet_compound") {
                if (booleans.NewData.ExitedMenu) {
                    wetSet += 1; // Definitely is new set
                } else if (booleans.NewData.ExitedPitBox) {
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
                currentTyreSet = RaceEngineerPlugin.GAME.IsACC ? (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.currentTyreSet") : -1;
            }

            if (newTyreName == null || newTyreName == Name) return;
            RaceEngineerPlugin.LogInfo($"Tyres changed from '{Name}' to '{newTyreName}'.");
            ResetValues();

            Name = newTyreName;
            if (!SetLaps.ContainsKey(Name)) {
                SetLaps[Name] = new Dictionary<int, int>();
            }

            if (car?.Info?.Tyres != null) {
                tyreInfo = car.Info.Tyres?[Name];
                if (tyreInfo != null) {
                    PresColorF.UpdateInterpolation(tyreInfo.IdealPres.F, tyreInfo.IdealPresRange.F);
                    PresColorR.UpdateInterpolation(tyreInfo.IdealPres.R, tyreInfo.IdealPresRange.R);
                    TempColorF.UpdateInterpolation(tyreInfo.IdealTemp.F, tyreInfo.IdealTempRange.F);
                    TempColorR.UpdateInterpolation(tyreInfo.IdealTemp.R, tyreInfo.IdealTempRange.R);
                } else {
                    ResetColors();
                }

                InitInputTyrePresPredictor(trackName, car.Name, car.Setup.advancedSetup.aeroBalance.brakeDuct, RaceEngineerPlugin.TrackGripStatus(pm), db);
            } else {
                RaceEngineerPlugin.LogInfo($"Current CarInfo '{car.Name}' doesn't have specs for tyres. Resetting to defaults.");
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

        private void PredictIdealInputPressures(double airtemp, double tracktemp, Booleans.Booleans booleans) {
            if (tyreInfo == null) {
                RaceEngineerPlugin.LogInfo($"Couldn't update ideal tyre pressures as 'tyreInfo == null'");
                for (int i = 0; i < 4; i++) {
                    PredictedIdealInputPres[i] = double.NaN;
                }
                return;
            }


            if (InputTyrePresPredictor != null && !updatingPresPredictor) {
                var preds = InputTyrePresPredictor.Predict(airtemp, tracktemp, tyreInfo.IdealPres.F, tyreInfo.IdealPres.R);
                for (int i = 0; i < 4; i++) {
                    PredictedIdealInputPres[i] = preds[i];
                }
            } else {
                for (int i = 0; i < 4; i++) {
                    PredictedIdealInputPres[i] = double.NaN;
                }
            }

        }

        private void InitInputTyrePresPredictor(string trackName, string carName, int[] brakeDucts, string track_grip_status, Database.Database db) {
            InputTyrePresPredictor = null;
            _ = Task.Run(() => {
                updatingPresPredictor = true;
                InputTyrePresPredictor = new InputTyrePresPredictor(trackName, carName, brakeDucts, Name, track_grip_status, db);
                updatingPresPredictor = false;
            });
            RaceEngineerPlugin.LogInfo("Started building tyre pres models.");
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
            InputTyrePresPredictor = null;
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

        public InputTyrePresPredictor(string trackName, string carName, int[] brakeDucts, string compound, string track_grip_status, Database.Database db) { 
            this.trackName = trackName;
            this.carName = carName;
            this.brakeDucts = brakeDucts;
            this.compound = compound;

            regressors = new ML.RidgeRegression[] {
                InitRegressor(0, track_grip_status, db), 
                InitRegressor(1, track_grip_status, db), 
                InitRegressor(2, track_grip_status, db), 
                InitRegressor(3, track_grip_status, db)
            };

            RaceEngineerPlugin.LogInfo($"Created InputTyrePresPredictor({trackName}, {carName}, [{brakeDucts[0]}, {brakeDucts[1]}], {compound})");
        }

        private ML.RidgeRegression InitRegressor(int tyre, string track_grip_status, Database.Database db) {
            var data = db.GetInputPresData(tyre, carName, trackName, tyre < 3 ? brakeDucts[0] : brakeDucts[1], compound, track_grip_status);
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