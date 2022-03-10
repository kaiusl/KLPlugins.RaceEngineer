using GameReaderCommon;
using SimHub.Plugins;
using System;
using RaceEngineerPlugin.Stats;

namespace RaceEngineerPlugin.Car {

    public class Tyres {
        public string Name { get; private set; }
        public double[] IdealInputPres { get; }
        public double[] PredictedIdealInputPres { get; }
        public double[] CurrentInputPres { get; }
        public double[] PresLoss { get; }
        public WheelsStats PresOverLap { get; }
        public WheelsStats TempOverLap { get; }
        public Color.ColorCalculator PresColorF { get; private set; }
        public Color.ColorCalculator PresColorR { get; private set; }
        public Color.ColorCalculator TempColorF { get; private set; }
        public Color.ColorCalculator TempColorR { get; private set; }
        public int SetLaps { get; private set; }
        public InputTyrePresPredictor inputTyrePresPredictor { get; private set; }

        private WheelsRunningStats presRunning = new WheelsRunningStats();
        private WheelsRunningStats tempRunning = new WheelsRunningStats();
        private TyreInfo tyreInfo = null;
        private double lastSampleTimeSec = DateTime.Now.Second;

        public Tyres() {
            RaceEngineerPlugin.LogInfo("Created new Tyres");
            PresOverLap = new WheelsStats();
            TempOverLap = new WheelsStats();
            IdealInputPres = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN};
            PredictedIdealInputPres = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN };
            CurrentInputPres = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN };
            PresLoss = new double[4] { 0.0, 0.0, 0.0, 0.0 };
            SetLaps = 0;
            ResetColors();
        }

        public static string GetTyreCompound(PluginManager pluginManager) {
            return (RaceEngineerPlugin.GAME.IsAC || RaceEngineerPlugin.GAME.IsACC) ? (string)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.TyreCompound") : null;
        }

        #region On... METHODS

        public void OnNewStint(PluginManager pluginManager, Database.Database db) {
            if (RaceEngineerPlugin.GAME.IsACC) {
                int tyreset = (int)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.currentTyreSet");
                SetLaps = db.GetLapsOnTyreset(tyreset);
            } else {
                SetLaps = 0;
            }
        }

        public void OnLapFinished(double airtemp, double tracktemp) { 
            SetLaps += 1;
            PresOverLap.Update(presRunning);
            TempOverLap.Update(tempRunning);
            UpdateIdealInputPressures(airtemp, tracktemp);
            presRunning.Reset();
            tempRunning.Reset();
        }

        public void OnRegularUpdate(PluginManager pm, GameData data, Car car, Booleans.Booleans booleans, string trackName, Database.Database db) {
            CheckCompoundChange(pm, car, trackName, db);
            CheckPresChange(data);
            UpdateOverLapData(data, booleans);
            PredictIdealInputPressures(data.NewData.AirTemperature, data.NewData.RoadTemperature);
        }

        #endregion

        #region PRIVATE METHODS

        private void CheckCompoundChange(PluginManager pluginManager, Car car, string trackName, Database.Database db) {
            string newTyreName = GetTyreCompound(pluginManager);
            if (newTyreName != null && newTyreName != Name && car.Info != null && car.Info.Tyres != null) {
                if (car.Info != null && car.Info.Tyres != null) {
                    RaceEngineerPlugin.LogInfo($"Tyres changed from '{Name}' to '{newTyreName}'.");
                    tyreInfo = car.Info.Tyres?[newTyreName];
                    if (tyreInfo != null) {
                        PresColorF.UpdateInterpolation(tyreInfo.IdealPres.F, tyreInfo.IdealPresRange.F);
                        PresColorR.UpdateInterpolation(tyreInfo.IdealPres.R, tyreInfo.IdealPresRange.R);
                        TempColorF.UpdateInterpolation(tyreInfo.IdealTemp.F, tyreInfo.IdealTempRange.F);
                        TempColorR.UpdateInterpolation(tyreInfo.IdealTemp.R, tyreInfo.IdealTempRange.R);
                    } else {
                        RaceEngineerPlugin.LogInfo($"Current CarInfo '{car.Name}' doesn't have specs for tyre '{newTyreName}'. Resetting to defaults.");
                        ResetColors();
                    }
                    ResetValues();
                    InitInputTyrePresPredictor(trackName, car.Name, car.Setup.advancedSetup.aeroBalance.brakeDuct, newTyreName, db);

                    Name = newTyreName;
                } else {
                    RaceEngineerPlugin.LogInfo($"Current CarInfo '{car.Name}' doesn't have specs for tyre '{newTyreName}'. Resetting to defaults.");
                    ResetColors();
                    ResetValues();
                    Name = null;
                }
            }
        }

        private void ResetPressureLoss() {
            for (int i = 0; i < 4; i++) {
                PresLoss[i] = 0.0;
            }
        }

        private void CheckPresChange(GameData data) {
            if (data.NewData.TyrePressureFrontLeft == 0) {
                return;
            }

            var presDelta = new double[4] {
                 data.NewData.TyrePressureFrontLeft - data.OldData.TyrePressureFrontLeft,
                 data.NewData.TyrePressureFrontRight - data.OldData.TyrePressureFrontRight,
                 data.NewData.TyrePressureRearLeft - data.OldData.TyrePressureRearLeft,
                 data.NewData.TyrePressureRearRight - data.OldData.TyrePressureRearRight
            };

            if (data.NewData.SpeedKmh < 10 && data.NewData.IsInPitLane == 1) {
                Func<double, bool> pred = v => Math.Abs(v) > 0.1;
                if (pred(presDelta[0]) || pred(presDelta[1]) || pred(presDelta[2]) || pred(presDelta[3])) {
                    RaceEngineerPlugin.LogInfo("Current input tyre pressures updated.");
                    CurrentInputPres[0] = data.NewData.TyrePressureFrontLeft;
                    CurrentInputPres[1] = data.NewData.TyrePressureFrontRight;
                    CurrentInputPres[2] = data.NewData.TyrePressureRearLeft;
                    CurrentInputPres[3] = data.NewData.TyrePressureRearRight;

                    ResetPressureLoss();

                }
            } else {
                for (int i = 0; i < 4; i++) {
                    if (presDelta[i] < -0.1) { 
                        PresLoss[i] += presDelta[i];
                    }
                }
            }
        }

        private void UpdateOverLapData(GameData data, Booleans.Booleans booleans) {
            double now = DateTime.Now.Second;

            // Add sample to counters
            if (booleans.NewData.IsMoving && booleans.NewData.IsOnTrack && lastSampleTimeSec != now) {
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

        private void PredictIdealInputPressures(double airtemp, double tracktemp) {
            if (tyreInfo != null) {
                var preds = inputTyrePresPredictor.Predict(airtemp, tracktemp, tyreInfo.IdealPres.F, tyreInfo.IdealPres.R);
                for (int i = 0; i < 4; i++) {
                    PredictedIdealInputPres[i] = preds[i];
                }
            } else {
                RaceEngineerPlugin.LogInfo($"Couldn't update ideal tyre pressures as 'tyreInfo == null'");
            }
        }

        private void InitInputTyrePresPredictor(string trackName, string carName, int[] brakeDucts, string compound, Database.Database db) {
            inputTyrePresPredictor = new InputTyrePresPredictor(trackName, carName, brakeDucts, compound, db);
            var preds = inputTyrePresPredictor.Predict(25, 35, 27.5, 27.5);
            for (int i = 0; i < 4; i++) {
                IdealInputPres[i] = preds[i];
            }
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
            Name = null;
            for (var i = 0; i < 4; i++) { 
                IdealInputPres[i] = double.NaN;
                CurrentInputPres[i] = double.NaN;
            }
            PresOverLap.Reset();
            TempOverLap.Reset();
            presRunning.Reset();
            tempRunning.Reset();
            inputTyrePresPredictor = null;
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

        public InputTyrePresPredictor(string trackName, string carName, int[] brakeDucts, string compound, Database.Database db) { 
            this.trackName = trackName;
            this.carName = carName;
            this.brakeDucts = brakeDucts;
            this.compound = compound;

            regressors = new ML.RidgeRegression[] {
                InitRegressor(0, db), InitRegressor(1, db), InitRegressor(2, db), InitRegressor(3, db)
            };

            RaceEngineerPlugin.LogInfo($"Created InputTyrePresPredictor({trackName}, {carName}, [{brakeDucts[0]}, {brakeDucts[1]}], {compound})");
        }

        private ML.RidgeRegression InitRegressor(int tyre, Database.Database db) {
            var data = db.GetInputPresData(tyre, carName, trackName, tyre < 3 ? brakeDucts[0] : brakeDucts[1], compound);
            if (data.Item2.Count != 0) {
                return new ML.RidgeRegression(data.Item1, data.Item2);
            } else {
                SimHub.Logging.Current.Info("Got zero values from GetInputPresData query.");
                return null;
            }            
        }

        public double[] Predict(double airtemp, double tracktemp, double idealPresFront, double idealPresRear) {
            var res = new double[4];
            for (int i = 0; i < 4; i++) {
                if (regressors[i] != null) {
                    res[i] = regressors[i].Predict(new double[] { i < 3 ? idealPresFront : idealPresRear, airtemp, tracktemp });
                } else { 
                    res[i] = double.NaN;
                }
            }
            //RaceEngineerPlugin.LogInfo($"Predicted input pressures at air={airtemp}, track={tracktemp} to be [{res[0]}, {res[1]}, {res[2]}, {res[3]}]");
            return res;
        }

    }

}