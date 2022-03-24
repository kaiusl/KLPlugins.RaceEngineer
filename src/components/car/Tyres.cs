using GameReaderCommon;
using SimHub.Plugins;
using System;
using RaceEngineerPlugin.Stats;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using RaceEngineerPlugin.RawData;
using ACSharedMemory.ACC.MMFModels;

namespace RaceEngineerPlugin.Car {

    public class Tyres {
        public static string[] Names = new string[4] { "FL", "FR", "RL", "RR" };

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
        public int CurrentTyreSet { get; private set; }


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


        private volatile bool _updatingPresPredictorDry = false;
        private volatile bool _updatingPresPredictorNowWet = false;
        private volatile bool _updatingPresPredictorFutureWet = false;
        private WheelsRunningStats _presRunning = new WheelsRunningStats();
        private WheelsRunningStats _tempRunning = new WheelsRunningStats();
        private TyreInfo _tyreInfo = null;
        private double _lastSampleTimeSec = DateTime.Now.Second;
        private int _wetSet = 0;

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
            PresColor = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
            TempColor = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
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
                PresColor[i] = RaceEngineerPlugin.DefColor;
                TempColor[i] = RaceEngineerPlugin.DefColor;
            }

            PresOverLap.Reset();
            TempOverLap.Reset();

            PresColorF = new Color.ColorCalculator(RaceEngineerPlugin.Settings.PresColor, RaceEngineerPlugin.Settings.TyrePresColorDefValues);
            PresColorR = new Color.ColorCalculator(RaceEngineerPlugin.Settings.PresColor, RaceEngineerPlugin.Settings.TyrePresColorDefValues);
            TempColorF = new Color.ColorCalculator(RaceEngineerPlugin.Settings.TempColor, RaceEngineerPlugin.Settings.TyreTempColorDefValues);
            TempColorR = new Color.ColorCalculator(RaceEngineerPlugin.Settings.TempColor, RaceEngineerPlugin.Settings.TyreTempColorDefValues);

            SetLaps.Clear();
            InputTyrePresPredictorDry = null;
            InputTyrePresPredictorNowWet = null;
            InputTyrePresPredictorFutureWet = null;

            _updatingPresPredictorDry = false;
            _updatingPresPredictorNowWet = false;
            _updatingPresPredictorFutureWet = false;
            _presRunning.Reset();
            _tempRunning.Reset();
            _tyreInfo = null;
            _wetSet = 0;
            CurrentTyreSet = 0;
        }

        public int GetCurrentSetLaps() {
            if (!RaceEngineerPlugin.Game.IsAcc || Name == null) return -1;
            return SetLaps?[Name]?[CurrentTyreSet] ?? -1;
        }

        #region On... METHODS

        public void OnSetupChange() {
            InputTyrePresPredictorDry = null;
            InputTyrePresPredictorNowWet = null;
            InputTyrePresPredictorFutureWet = null;
            _updatingPresPredictorDry = false;
            _updatingPresPredictorNowWet = false;
            _updatingPresPredictorFutureWet = false;
        }

        public void OnNewStint() {
            if (RaceEngineerPlugin.Game.IsAcc) {
                if (!SetLaps[Name].ContainsKey(CurrentTyreSet)) {
                    SetLaps[Name][CurrentTyreSet] = 0;
                }
            }
        }

        public void OnLapFinished(double airtemp, double tracktemp) {
            if (RaceEngineerPlugin.Game.IsAcc) {
                SetLaps[Name][CurrentTyreSet] += 1;
            }

            PresOverLap.Update(_presRunning);
            TempOverLap.Update(_tempRunning);
            UpdateIdealInputPressures(airtemp, tracktemp);
            _presRunning.Reset();
            _tempRunning.Reset();
        }

        public void OnLapFinishedAfterInsert() {
            for (int i = 0; i < 4; i++) {
                PresLossLap[i] = false;
            }
        }

        public void OnRegularUpdate(GameData data, Values v) {
            CheckCompoundChange(data, v, data.NewData.TrackId);
            CheckPresChange(data, v.Booleans);
            UpdateOverLapData(data, v.Booleans);
            PredictIdealInputPressures(v);
            UpdateColors(data, v.Booleans.NewData.IsInMenu);
            

        }

        #endregion

        #region PRIVATE METHODS

        private void UpdateColors(GameData data, bool isInMenu) {
            if (!isInMenu) {
                if ((WheelFlags.Color & RaceEngineerPlugin.Settings.TyrePresFlags) != 0) {
                    PresColor[0] = PresColorF.GetColor(data.NewData.TyrePressureFrontLeft).ToHEX();
                    PresColor[1] = PresColorF.GetColor(data.NewData.TyrePressureFrontRight).ToHEX();
                    PresColor[2] = PresColorR.GetColor(data.NewData.TyrePressureRearLeft).ToHEX();
                    PresColor[3] = PresColorR.GetColor(data.NewData.TyrePressureRearRight).ToHEX();
                }

                if ((WheelFlags.Color & RaceEngineerPlugin.Settings.TyreTempFlags) != 0) {
                    TempColor[0] = TempColorF.GetColor(data.NewData.TyreTemperatureFrontLeft).ToHEX();
                    TempColor[1] = TempColorF.GetColor(data.NewData.TyreTemperatureFrontRight).ToHEX();
                    TempColor[2] = TempColorR.GetColor(data.NewData.TyreTemperatureRearLeft).ToHEX();
                    TempColor[3] = TempColorR.GetColor(data.NewData.TyreTemperatureRearRight).ToHEX();
                }
            }
        }

        private void CheckCompoundChange(GameData data, Values v, string trackName) {
            // Pads can change at two moments:
            //    a) If we exit garage
            //    b) If we change tyres in pit stop.

            if (Name != null && !(v.Booleans.NewData.ExitedMenu || v.Booleans.NewData.ExitedPitBox)) return;

            string newTyreName = v.RawData.NewData.Graphics.TyreCompound;

            if (newTyreName == "wet_compound") {
                if (v.Booleans.NewData.ExitedMenu) {
                    _wetSet += 1; // Definitely is new set
                } else if (v.Booleans.NewData.ExitedPitBox) {
                    // Could be new set but there is really no way to tell since wet sets are not numbered
                    // Since tyre change takes 30 seconds, let's assume that if pitstop is 29s or longer that we changed tyres. 
                    // Not 100% true since with repairs or brake change we can get longer pitstops
                    // But we do know that if pit was shorter than 30s, we couldn't have changed tyres.
                    RaceEngineerPlugin.LogInfo($"Exited pit box: pit time = {data.OldData.IsInPitSince}");
                    if (data.OldData.IsInPitSince > 29) {
                        _wetSet += 1;
                    }
                }
                CurrentTyreSet = _wetSet;
            } else {
                CurrentTyreSet = RaceEngineerPlugin.Game.IsAcc ? v.RawData.NewData.Graphics.currentTyreSet : -1;
            }

            if (newTyreName == null || newTyreName == Name) return;
            RaceEngineerPlugin.LogInfo($"Tyres changed from '{Name}' to '{newTyreName}'.");
            ResetValues();

            Name = newTyreName;
            if (!SetLaps.ContainsKey(Name)) {
                SetLaps[Name] = new Dictionary<int, int>();
            }

            if (v.Car?.Info?.Tyres != null) {
                _tyreInfo = v.Car.Info.Tyres?[Name];
                if (_tyreInfo != null) {
                    PresColorF.UpdateInterpolation(_tyreInfo.IdealPres.F, _tyreInfo.IdealPresRange.F);
                    PresColorR.UpdateInterpolation(_tyreInfo.IdealPres.R, _tyreInfo.IdealPresRange.R);
                    TempColorF.UpdateInterpolation(_tyreInfo.IdealTemp.F, _tyreInfo.IdealTempRange.F);
                    TempColorR.UpdateInterpolation(_tyreInfo.IdealTemp.R, _tyreInfo.IdealTempRange.R);
                } else {
                    ResetColors();
                }
            } else {
                RaceEngineerPlugin.LogInfo($"Current CarInfo '{v.Car.Name}' doesn't have specs for tyres. Resetting to defaults.");
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
                    CurrentInputPres[0] = Math.Ceiling(data.NewData.TyrePressureFrontLeft * 10.0) / 10.0;
                    CurrentInputPres[1] = Math.Ceiling(data.NewData.TyrePressureFrontRight * 10.0) / 10.0;
                    CurrentInputPres[2] = Math.Ceiling(data.NewData.TyrePressureRearLeft * 10.0) / 10.0;
                    CurrentInputPres[3] = Math.Ceiling(data.NewData.TyrePressureRearRight * 10.0) / 10.0;

                    RaceEngineerPlugin.LogInfo($"Current input tyre pressures updated to [{CurrentInputPres[0]}, {CurrentInputPres[1]}, {CurrentInputPres[2]}, {CurrentInputPres[3]}].");
                    ResetPressureLoss();

                }
            } else {
                for (int i = 0; i < 4; i++) {
                    if (presDelta[i] < -PRESS_LOSS_THRESHOLD) { 
                        PresLoss[i] += presDelta[i];
                        PresLossLap[i] = true;
                        RaceEngineerPlugin.LogInfo($"Pressure loss on {Names[i]} by {presDelta[i]}.");
                    }
                }
            }
        }

        private void UpdateOverLapData(GameData data, Booleans.Booleans booleans) {
            // Add sample to counters
            if (booleans.NewData.IsMoving && booleans.NewData.IsOnTrack) {
                double now = data.FrameTime.Second;
                if (_lastSampleTimeSec == now) return;
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

                _presRunning.Update(currentPres);
                _tempRunning.Update(currentTemp);

                _lastSampleTimeSec = now;
            }
        }

        private void UpdateIdealInputPressures(double airtemp, double tracktemp) {
            if (_tyreInfo != null) {    
                for (int i = 0; i < 4; i++) {
                    IdealInputPres[i] = CurrentInputPres[i] + (_tyreInfo.IdealPres[i] - PresOverLap[i].Avg);
                }
            } else {
                RaceEngineerPlugin.LogInfo($"Couldn't update ideal tyre pressures as 'tyreInfo == null'");
            }
        }

        private void PredictIdealInputPressures(Values v) {
            if (_tyreInfo == null || v.Weather.AirTemp == 0.0) {
               // RaceEngineerPlugin.LogInfo($"Couldn't update ideal tyre pressures as 'tyreInfo == null' || 'AirTemp == 0.0'");
                for (int i = 0; i < 4; i++) {
                    PredictedIdealInputPresDry[i] = double.NaN;
                    PredictedIdealInputPresNowWet[i] = double.NaN;
                    PredictedIdealInputPresFutureWet[i] = double.NaN;
                }
                return;
            }

            if (!_updatingPresPredictorDry 
                && (v.RawData.NewData.Graphics.rainIntensity == ACC_RAIN_INTENSITY.ACC_NO_RAIN 
                    || v.RawData.NewData.Graphics.rainIntensityIn10min == ACC_RAIN_INTENSITY.ACC_NO_RAIN 
                    || v.RawData.NewData.Graphics.rainIntensityIn30min == ACC_RAIN_INTENSITY.ACC_NO_RAIN
                )
            ) {
                if (InputTyrePresPredictorDry != null) {
                    var preds = InputTyrePresPredictorDry.Predict(v.Weather.AirTemp, v.Weather.TrackTemp, _tyreInfo.IdealPres.F, _tyreInfo.IdealPres.R);
                    preds.CopyTo(PredictedIdealInputPresDry, 0);
                } else {
                    if (v.Car.Setup != null) {
                        InitInputTyrePresPredictorDry(v.Track.Name, v.Car.Name, v.Car.Setup.advancedSetup.aeroBalance.brakeDuct, v.Db);
                        for (int i = 0; i < 4; i++) {
                            PredictedIdealInputPresDry[i] = double.NaN;
                        }
                    }
                    
                }
            }

            if (!_updatingPresPredictorNowWet && v.RawData.NewData.Graphics.rainIntensity != ACC_RAIN_INTENSITY.ACC_NO_RAIN) {
                if (v.RawData.NewData.Graphics.rainIntensity != v.RawData.OldData.Graphics.rainIntensity || InputTyrePresPredictorNowWet == null) {
                    if (v.Car.Setup != null) {
                        InitInputTyrePresPredictorNowWet(v.Track.Name, v.Car.Name, v.Car.Setup.advancedSetup.aeroBalance.brakeDuct, v.RawData, v.Db);
                    }
                    for (int i = 0; i < 4; i++) {
                        PredictedIdealInputPresNowWet[i] = double.NaN;
                    }
                } else {
                    var preds = InputTyrePresPredictorNowWet.Predict(v.Weather.AirTemp, v.Weather.TrackTemp, _tyreInfo.IdealPres.F, _tyreInfo.IdealPres.R);
                    preds.CopyTo(PredictedIdealInputPresNowWet, 0);
                }
            }

            if (!_updatingPresPredictorFutureWet && (v.RawData.NewData.Graphics.rainIntensityIn30min != ACC_RAIN_INTENSITY.ACC_NO_RAIN || v.RawData.NewData.Graphics.rainIntensityIn10min != ACC_RAIN_INTENSITY.ACC_NO_RAIN)) {
                if (v.RawData.NewData.Graphics.rainIntensityIn30min != v.RawData.OldData.Graphics.rainIntensityIn30min || InputTyrePresPredictorFutureWet == null) {
                    if (v.Car.Setup != null) {
                        InitInputTyrePresPredictorFutureWet(v.Track.Name, v.Car.Name, v.Car.Setup.advancedSetup.aeroBalance.brakeDuct, v.RawData, v.Db);
                    }
                    for (int i = 0; i < 4; i++) {
                        PredictedIdealInputPresFutureWet[i] = double.NaN;
                    }
                } else {
                    var preds = InputTyrePresPredictorFutureWet.Predict(v.Weather.AirTemp, v.Weather.TrackTemp, _tyreInfo.IdealPres.F, _tyreInfo.IdealPres.R);
                    preds.CopyTo(PredictedIdealInputPresFutureWet, 0);
                }
            }
        }


        private void InitInputTyrePresPredictorDry(string trackName, string carName, int[] brakeDucts, Database.Database db) {
            InputTyrePresPredictorDry = null;
            _ = Task.Run(() => {
                _updatingPresPredictorDry = true;
                InputTyrePresPredictorDry = new InputTyrePresPredictor(trackName, carName, brakeDucts, "dry_compound", ACC_RAIN_INTENSITY.ACC_NO_RAIN, $"(0, 1, 2)", db);
                _updatingPresPredictorDry = false;
            });
            RaceEngineerPlugin.LogInfo("Started building dry tyre pres models.");
        }

        private void InitInputTyrePresPredictorNowWet(string trackName, string carName, int[] brakeDucts, ACCRawData rawData,  Database.Database db) {
            InputTyrePresPredictorNowWet = null;
            _ = Task.Run(() => {
                _updatingPresPredictorNowWet = true;
                InputTyrePresPredictorNowWet = new InputTyrePresPredictor(trackName, carName, brakeDucts, "wet_compound", rawData.NewData.Graphics.rainIntensity, $"({(int)rawData.NewData.Graphics.trackGripStatus})", db);
                _updatingPresPredictorNowWet = false;
            });
            RaceEngineerPlugin.LogInfo("Started building now wet tyre pres models.");
        }

        private void InitInputTyrePresPredictorFutureWet(string trackName, string carName, int[] brakeDucts, ACCRawData rawData, Database.Database db) {
            InputTyrePresPredictorFutureWet = null;
            _ = Task.Run(() => {
                _updatingPresPredictorFutureWet = true;

                var futureTrackGrip = ACC_TRACK_GRIP_STATUS.ACC_WET;
                if (rawData.NewData.Graphics.rainIntensityIn30min == ACC_RAIN_INTENSITY.ACC_THUNDERSTORM) {
                    futureTrackGrip = ACC_TRACK_GRIP_STATUS.ACC_FLOODED;
                } else if (rawData.NewData.Graphics.rainIntensityIn30min == ACC_RAIN_INTENSITY.ACC_DRIZZLE
                    && (   rawData.NewData.Graphics.rainIntensity == ACC_RAIN_INTENSITY.ACC_NO_RAIN
                        || rawData.NewData.Graphics.rainIntensity == ACC_RAIN_INTENSITY.ACC_DRIZZLE
                        || rawData.NewData.Graphics.rainIntensity == ACC_RAIN_INTENSITY.ACC_LIGHT_RAIN
                    )
                ) {
                    futureTrackGrip = ACC_TRACK_GRIP_STATUS.ACC_DAMP;
                }

                InputTyrePresPredictorFutureWet = new InputTyrePresPredictor(trackName, carName, brakeDucts, "wet_compound", rawData.NewData.Graphics.rainIntensityIn30min, $"({(int)futureTrackGrip})", db);
                _updatingPresPredictorFutureWet = false;
            });
            RaceEngineerPlugin.LogInfo("Started building future wet tyre pres models.");
        }


        private void ResetColors() {
            RaceEngineerPlugin.LogInfo("Tyres.ResetColors()");
            PresColorF = new Color.ColorCalculator(RaceEngineerPlugin.Settings.PresColor, RaceEngineerPlugin.Settings.TyrePresColorDefValues);
            PresColorR = new Color.ColorCalculator(RaceEngineerPlugin.Settings.PresColor, RaceEngineerPlugin.Settings.TyrePresColorDefValues);
            TempColorF = new Color.ColorCalculator(RaceEngineerPlugin.Settings.TempColor, RaceEngineerPlugin.Settings.TyreTempColorDefValues);
            TempColorR = new Color.ColorCalculator(RaceEngineerPlugin.Settings.TempColor, RaceEngineerPlugin.Settings.TyreTempColorDefValues);
        }

        private void ResetValues() {
            RaceEngineerPlugin.LogInfo("Tyres.ResetValues()");
            for (var i = 0; i < 4; i++) { 
                IdealInputPres[i] = double.NaN;
            }
            PresOverLap.Reset();
            TempOverLap.Reset();
            _presRunning.Reset();
            _tempRunning.Reset();
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

        public InputTyrePresPredictor(string trackName, string carName, int[] brakeDucts, string compound, ACC_RAIN_INTENSITY rain_intensity, string trackGrip, Database.Database db) { 
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

        private ML.RidgeRegression InitRegressor(int tyre, ACC_RAIN_INTENSITY rainIntensity, string trackGrip, Database.Database db) {
            var data = db.GetInputPresData(tyre, carName, trackName, tyre < 2 ? brakeDucts[0] : brakeDucts[1], compound, trackGrip, rainIntensity);
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