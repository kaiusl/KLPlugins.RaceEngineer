using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ACSharedMemory.ACC.MMFModels;

using GameReaderCommon;

using KLPlugins.RaceEngineer.Stats;

namespace KLPlugins.RaceEngineer.Car {

    public class Tyres {
        public static string[] Names = new string[4] { "FL", "FR", "RL", "RR" };

        public string? Name { get; private set; }

        public double[] IdealInputPres { get; }
        public double[] PredictedIdealInputPresDry { get; }
        public double[] PredictedIdealInputPresNowWet { get; }
        public double[] PredictedIdealInputPresFutureWet { get; }
        public double[] CurrentInputPres { get; }
        public double[] PresLoss { get; }
        public bool[] PresLossLap { get; }

        public string[] PresColor { get; private set; }
        public string[] PresColorMin { get; private set; }
        public string[] PresColorMax { get; private set; }
        public string[] PresColorAvg { get; private set; }

        public string[] TempColor { get; private set; }
        public string[] TempColorMin { get; private set; }
        public string[] TempColorMax { get; private set; }
        public string[] TempColorAvg { get; private set; }

        public int CurrentTyreSet { get; private set; }

        public WheelsStats PresOverLap { get; }
        public WheelsStats TempOverLap { get; }

        public Color.ColorCalculator? PresColorF { get; private set; }
        public Color.ColorCalculator? PresColorR { get; private set; }
        public Color.ColorCalculator? TempColorF { get; private set; }
        public Color.ColorCalculator? TempColorR { get; private set; }

        public Dictionary<string, Dictionary<int, int>> SetLaps { get; private set; }

        public InputTyrePresPredictor? InputTyrePresPredictorDry { get; private set; }
        public InputTyrePresPredictor? InputTyrePresPredictorNowWet { get; private set; }
        public InputTyrePresPredictor? InputTyrePresPredictorFutureWet { get; private set; }


        private volatile bool _updatingPresPredictorDry = false;
        private volatile bool _updatingPresPredictorNowWet = false;
        private volatile bool _updatingPresPredictorFutureWet = false;
        private WheelsRunningStats _presRunning = new WheelsRunningStats();
        private WheelsRunningStats _tempRunning = new WheelsRunningStats();
        private TyreInfo? _tyreInfo = null;
        private double _lastSampleTimeSec = DateTime.Now.Second;
        private int _wetSet = 0;

        public Tyres() {
            RaceEngineerPlugin.LogInfo("Created new Tyres");
            this.PresOverLap = new WheelsStats();
            this.TempOverLap = new WheelsStats();
            this.IdealInputPres = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN };
            this.PredictedIdealInputPresDry = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN };
            this.PredictedIdealInputPresNowWet = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN };
            this.PredictedIdealInputPresFutureWet = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN };
            this.CurrentInputPres = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN };
            this.PresLoss = new double[4] { 0.0, 0.0, 0.0, 0.0 };
            this.PresLossLap = new bool[4] { false, false, false, false };
            this.SetLaps = new Dictionary<string, Dictionary<int, int>>();
            this.PresColor = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
            this.PresColorMin = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
            this.PresColorMax = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
            this.PresColorAvg = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
            this.TempColor = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
            this.TempColorMin = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
            this.TempColorMax = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
            this.TempColorAvg = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
            this.Reset();
        }

        public void Reset() {
            this.Name = null;

            for (int i = 0; i < 4; i++) {
                this.IdealInputPres[i] = double.NaN;
                this.PredictedIdealInputPresDry[i] = double.NaN;
                this.PredictedIdealInputPresNowWet[i] = double.NaN;
                this.PredictedIdealInputPresFutureWet[i] = double.NaN;
                this.CurrentInputPres[i] = double.NaN;
                this.PresLoss[i] = 0.0;
                this.PresLossLap[i] = false;
                this.PresColor[i] = RaceEngineerPlugin.DefColor;
                this.PresColorMin[i] = RaceEngineerPlugin.DefColor;
                this.PresColorMax[i] = RaceEngineerPlugin.DefColor;
                this.PresColorAvg[i] = RaceEngineerPlugin.DefColor;
                this.TempColor[i] = RaceEngineerPlugin.DefColor;
                this.TempColorMin[i] = RaceEngineerPlugin.DefColor;
                this.TempColorMax[i] = RaceEngineerPlugin.DefColor;
                this.TempColorAvg[i] = RaceEngineerPlugin.DefColor;
            }

            this.PresOverLap.Reset();
            this.TempOverLap.Reset();

            this.PresColorF = new Color.ColorCalculator(RaceEngineerPlugin.Settings.PresColor, RaceEngineerPlugin.Settings.TyrePresColorDefValues);
            this.PresColorR = new Color.ColorCalculator(RaceEngineerPlugin.Settings.PresColor, RaceEngineerPlugin.Settings.TyrePresColorDefValues);
            this.TempColorF = new Color.ColorCalculator(RaceEngineerPlugin.Settings.TempColor, RaceEngineerPlugin.Settings.TyreTempColorDefValues);
            this.TempColorR = new Color.ColorCalculator(RaceEngineerPlugin.Settings.TempColor, RaceEngineerPlugin.Settings.TyreTempColorDefValues);

            this.SetLaps.Clear();
            this.InputTyrePresPredictorDry = null;
            this.InputTyrePresPredictorNowWet = null;
            this.InputTyrePresPredictorFutureWet = null;

            this._updatingPresPredictorDry = false;
            this._updatingPresPredictorNowWet = false;
            this._updatingPresPredictorFutureWet = false;
            this._presRunning.Reset();
            this._tempRunning.Reset();
            this._tyreInfo = null;
            this._wetSet = 0;
            this.CurrentTyreSet = 0;
        }

        public int GetCurrentSetLaps() {
            if (!RaceEngineerPlugin.Game.IsAcc || this.Name == null) return -1;
            if (this.SetLaps.ContainsKey(this.Name) && this.SetLaps[this.Name].ContainsKey(this.CurrentTyreSet)) {
                return this.SetLaps[this.Name][this.CurrentTyreSet];
            }
            return -1;
        }

        #region On... METHODS

        public void OnSetupChange() {
            this.InputTyrePresPredictorDry = null;
            this.InputTyrePresPredictorNowWet = null;
            this.InputTyrePresPredictorFutureWet = null;
            this._updatingPresPredictorDry = false;
            this._updatingPresPredictorNowWet = false;
            this._updatingPresPredictorFutureWet = false;
        }

        public void OnNewStint() {
            if (RaceEngineerPlugin.Game.IsAcc) {
                if (this.Name == null) return;

                if (!this.SetLaps[this.Name].ContainsKey(this.CurrentTyreSet)) {
                    this.SetLaps[this.Name][this.CurrentTyreSet] = 0;
                }
            }
        }

        public void OnLapFinished(Values v) {
            if (RaceEngineerPlugin.Game.IsAcc && this.Name != null) {
                this.SetLaps[this.Name][this.CurrentTyreSet] += 1;
            }

            this.PresOverLap.Update(this._presRunning);
            this.TempOverLap.Update(this._tempRunning);
            this.UpdateIdealInputPressures(v.Weather.AirTemp, v.Weather.TrackTemp);
            this.UpdateOverLapColors(v);
            this._presRunning.Reset();
            this._tempRunning.Reset();
        }

        public void OnLapFinishedAfterInsert() {
            for (int i = 0; i < 4; i++) {
                this.PresLossLap[i] = false;
            }
        }

        public void OnRegularUpdate(GameData data, Values v) {
            CheckCompoundChange(data, v, data.NewData.TrackId);
            this.CheckPresChange(data, v.Booleans);
            this.UpdateOverLapData(data, v.Booleans);
            this.PredictIdealInputPressures(data, v);
            this.UpdateColors(data, v.Booleans.NewData.IsInMenu);


        }

        #endregion

        #region PRIVATE METHODS

        private void UpdateColors(GameData data, bool isInMenu) {
            if (!isInMenu) {
                if ((WheelFlags.Color & RaceEngineerPlugin.Settings.TyrePresFlags) != 0 && this.PresColorF != null && this.PresColorR != null) {
                    this.PresColor[0] = this.PresColorF.GetColor(data.NewData.TyrePressureFrontLeft).ToHEX();
                    this.PresColor[1] = this.PresColorF.GetColor(data.NewData.TyrePressureFrontRight).ToHEX();
                    this.PresColor[2] = this.PresColorR.GetColor(data.NewData.TyrePressureRearLeft).ToHEX();
                    this.PresColor[3] = this.PresColorR.GetColor(data.NewData.TyrePressureRearRight).ToHEX();
                }

                if ((WheelFlags.Color & RaceEngineerPlugin.Settings.TyreTempFlags) != 0 && this.TempColorF != null && this.TempColorR != null) {
                    this.TempColor[0] = this.TempColorF.GetColor(data.NewData.TyreTemperatureFrontLeft).ToHEX();
                    this.TempColor[1] = this.TempColorF.GetColor(data.NewData.TyreTemperatureFrontRight).ToHEX();
                    this.TempColor[2] = this.TempColorR.GetColor(data.NewData.TyreTemperatureRearLeft).ToHEX();
                    this.TempColor[3] = this.TempColorR.GetColor(data.NewData.TyreTemperatureRearRight).ToHEX();
                }
            }
        }

        private void UpdateOverLapColors(Values v) {
            if ((WheelFlags.MinColor & RaceEngineerPlugin.Settings.TyrePresFlags) != 0 && this.PresColorF != null) {
                this.PresColorMin[0] = this.PresColorF.GetColor(this.PresOverLap[0].Min).ToHEX();
                this.PresColorMin[1] = this.PresColorF.GetColor(this.PresOverLap[1].Min).ToHEX();
                this.PresColorMin[2] = this.PresColorF.GetColor(this.PresOverLap[2].Min).ToHEX();
                this.PresColorMin[3] = this.PresColorF.GetColor(this.PresOverLap[3].Min).ToHEX();
            }
            if ((WheelFlags.MaxColor & RaceEngineerPlugin.Settings.TyrePresFlags) != 0 && this.PresColorF != null) {
                this.PresColorMax[0] = this.PresColorF.GetColor(this.PresOverLap[0].Max).ToHEX();
                this.PresColorMax[1] = this.PresColorF.GetColor(this.PresOverLap[1].Max).ToHEX();
                this.PresColorMax[2] = this.PresColorF.GetColor(this.PresOverLap[2].Max).ToHEX();
                this.PresColorMax[3] = this.PresColorF.GetColor(this.PresOverLap[3].Max).ToHEX();
            }
            if ((WheelFlags.AvgColor & RaceEngineerPlugin.Settings.TyrePresFlags) != 0 && this.PresColorF != null) {
                this.PresColorAvg[0] = this.PresColorF.GetColor(this.PresOverLap[0].Avg).ToHEX();
                this.PresColorAvg[1] = this.PresColorF.GetColor(this.PresOverLap[1].Avg).ToHEX();
                this.PresColorAvg[2] = this.PresColorF.GetColor(this.PresOverLap[2].Avg).ToHEX();
                this.PresColorAvg[3] = this.PresColorF.GetColor(this.PresOverLap[3].Avg).ToHEX();
            }

            if ((WheelFlags.MinColor & RaceEngineerPlugin.Settings.TyreTempFlags) != 0 && this.TempColorF != null) {
                this.TempColorMin[0] = this.TempColorF.GetColor(this.TempOverLap[0].Min).ToHEX();
                this.TempColorMin[1] = this.TempColorF.GetColor(this.TempOverLap[1].Min).ToHEX();
                this.TempColorMin[2] = this.TempColorF.GetColor(this.TempOverLap[2].Min).ToHEX();
                this.TempColorMin[3] = this.TempColorF.GetColor(this.TempOverLap[3].Min).ToHEX();
            }
            if ((WheelFlags.MaxColor & RaceEngineerPlugin.Settings.TyreTempFlags) != 0 && this.TempColorF != null) {
                this.TempColorMax[0] = this.TempColorF.GetColor(this.TempOverLap[0].Max).ToHEX();
                this.TempColorMax[1] = this.TempColorF.GetColor(this.TempOverLap[1].Max).ToHEX();
                this.TempColorMax[2] = this.TempColorF.GetColor(this.TempOverLap[2].Max).ToHEX();
                this.TempColorMax[3] = this.TempColorF.GetColor(this.TempOverLap[3].Max).ToHEX();
            }
            if ((WheelFlags.AvgColor & RaceEngineerPlugin.Settings.TyreTempFlags) != 0 && this.TempColorF != null) {
                this.TempColorAvg[0] = this.TempColorF.GetColor(this.TempOverLap[0].Avg).ToHEX();
                this.TempColorAvg[1] = this.TempColorF.GetColor(this.TempOverLap[1].Avg).ToHEX();
                this.TempColorAvg[2] = this.TempColorF.GetColor(this.TempOverLap[2].Avg).ToHEX();
                this.TempColorAvg[3] = this.TempColorF.GetColor(this.TempOverLap[3].Avg).ToHEX();
            }

        }

        private void CheckCompoundChange(GameData data, Values v, string trackName) {
            // Pads can change at two moments:
            //    a) If we exit garage
            //    b) If we change tyres in pit stop.

            if (this.Name != null && !(v.Booleans.NewData.ExitedMenu || v.Booleans.NewData.ExitedPitBox)) return;

            string newTyreName;

            if (RaceEngineerPlugin.Game.IsAcc) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();

                newTyreName = rawDataNew.Graphics.TyreCompound;
                if (newTyreName == "wet_compound") {
                    if (v.Booleans.NewData.ExitedMenu) {
                        this._wetSet += 1; // Definitely is new set
                    } else if (v.Booleans.NewData.ExitedPitBox) {
                        // Could be new set but there is really no way to tell since wet sets are not numbered
                        // Since tyre change takes 30 seconds, let's assume that if pitstop is 29s or longer that we changed tyres. 
                        // Not 100% true since with repairs or brake change we can get longer pitstops
                        // But we do know that if pit was shorter than 30s, we couldn't have changed tyres.
                        RaceEngineerPlugin.LogInfo($"Exited pit box: pit time = {data.OldData.IsInPitSince}");
                        if (data.OldData.IsInPitSince > 29) {
                            this._wetSet += 1;
                        }
                    }
                    this.CurrentTyreSet = this._wetSet;
                } else {
                    this.CurrentTyreSet = rawDataNew.Graphics.currentTyreSet;
                }
            } else {
                // TODO: figure out other games
                newTyreName = "unknown";
                this.CurrentTyreSet = -1;
            }



            if (newTyreName == null || newTyreName == this.Name) return;
            RaceEngineerPlugin.LogInfo($"Tyres changed from '{this.Name}' to '{newTyreName}'.");
            this.ResetValues();

            this.Name = newTyreName;
            if (!this.SetLaps.ContainsKey(this.Name)) {
                this.SetLaps[this.Name] = new Dictionary<int, int>();
            }

            if (v.Car.Info?.Tyres != null) {
                this._tyreInfo = v.Car.Info.Tyres?[this.Name];
                if (this._tyreInfo != null) {
                    if (this._tyreInfo.IdealPres?.F != null && this._tyreInfo.IdealPresRange?.F != null) {
                        this.PresColorF?.UpdateInterpolation(this._tyreInfo.IdealPres.F, this._tyreInfo.IdealPresRange.F);
                    }
                    if (this._tyreInfo.IdealPres?.R != null && this._tyreInfo.IdealPresRange?.R != null) {
                        this.PresColorR?.UpdateInterpolation(this._tyreInfo.IdealPres.R, this._tyreInfo.IdealPresRange.R);
                    }
                    if (this._tyreInfo.IdealTemp?.F != null && this._tyreInfo.IdealTempRange?.F != null) {
                        this.TempColorF?.UpdateInterpolation(this._tyreInfo.IdealTemp.F, this._tyreInfo.IdealTempRange.F);
                    }
                    if (this._tyreInfo.IdealTemp?.R != null && this._tyreInfo.IdealTempRange?.R != null) {
                        this.TempColorR?.UpdateInterpolation(this._tyreInfo.IdealTemp.R, this._tyreInfo.IdealTempRange.R);
                    }

                } else {
                    this.ResetColors();
                }
            } else {
                RaceEngineerPlugin.LogInfo($"Current CarInfo '{v.Car.Name}' doesn't have specs for tyres. Resetting to defaults.");
            }
        }

        private void ResetPressureLoss() {
            for (int i = 0; i < 4; i++) {
                this.PresLoss[i] = 0.0;
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
                    this.CurrentInputPres[0] = Math.Ceiling(data.NewData.TyrePressureFrontLeft * 10.0) / 10.0;
                    this.CurrentInputPres[1] = Math.Ceiling(data.NewData.TyrePressureFrontRight * 10.0) / 10.0;
                    this.CurrentInputPres[2] = Math.Ceiling(data.NewData.TyrePressureRearLeft * 10.0) / 10.0;
                    this.CurrentInputPres[3] = Math.Ceiling(data.NewData.TyrePressureRearRight * 10.0) / 10.0;

                    RaceEngineerPlugin.LogInfo($"Current input tyre pressures updated to [{this.CurrentInputPres[0]}, {this.CurrentInputPres[1]}, {this.CurrentInputPres[2]}, {this.CurrentInputPres[3]}].");
                    this.ResetPressureLoss();

                }
            } else {
                for (int i = 0; i < 4; i++) {
                    if (presDelta[i] < -PRESS_LOSS_THRESHOLD) {
                        this.PresLoss[i] += presDelta[i];
                        this.PresLossLap[i] = true;
                        RaceEngineerPlugin.LogInfo($"Pressure loss on {Names[i]} by {presDelta[i]}.");
                    }
                }
            }
        }

        private void UpdateOverLapData(GameData data, Booleans.Booleans booleans) {
            // Add sample to counters
            if (booleans.NewData.IsMoving && booleans.NewData.IsOnTrack) {
                double now = data.FrameTime.Second;
                if (this._lastSampleTimeSec == now) return;
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

                this._presRunning.Update(currentPres);
                this._tempRunning.Update(currentTemp);

                this._lastSampleTimeSec = now;
            }
        }

        private void UpdateIdealInputPressures(double airtemp, double tracktemp) {
            if (this._tyreInfo?.IdealPres != null) {
                for (int i = 0; i < 4; i++) {
                    this.IdealInputPres[i] = this.CurrentInputPres[i] + (this._tyreInfo.IdealPres[i] - this.PresOverLap[i].Avg);
                }
            } else {
                RaceEngineerPlugin.LogInfo($"Couldn't update ideal tyre pressures as 'tyreInfo == null'");
            }
        }

        private void PredictIdealInputPressures(GameData data, Values v) {
            if (this._tyreInfo == null || v.Weather.AirTemp == 0.0) {
                // RaceEngineerPlugin.LogInfo($"Couldn't update ideal tyre pressures as 'tyreInfo == null' || 'AirTemp == 0.0'");
                for (int i = 0; i < 4; i++) {
                    this.PredictedIdealInputPresDry[i] = double.NaN;
                    this.PredictedIdealInputPresNowWet[i] = double.NaN;
                    this.PredictedIdealInputPresFutureWet[i] = double.NaN;
                }
                return;
            }

            if (RaceEngineerPlugin.Game.IsAcc) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();
                var rawDataOld = (ACSharedMemory.ACC.Reader.ACCRawData)data.OldData.GetRawDataObject();

                if (!this._updatingPresPredictorDry
                    && (rawDataNew.Graphics.rainIntensity == ACC_RAIN_INTENSITY.ACC_NO_RAIN
                        || rawDataNew.Graphics.rainIntensityIn10min == ACC_RAIN_INTENSITY.ACC_NO_RAIN
                        || rawDataNew.Graphics.rainIntensityIn30min == ACC_RAIN_INTENSITY.ACC_NO_RAIN
                    )
                ) {
                    if (this.InputTyrePresPredictorDry != null && this._tyreInfo.IdealPres != null) {
                        var preds = this.InputTyrePresPredictorDry.Predict(v.Weather.AirTemp, v.Weather.TrackTemp, this._tyreInfo.IdealPres.F, this._tyreInfo.IdealPres.R);
                        preds.CopyTo(this.PredictedIdealInputPresDry, 0);
                    } else {
                        if (v.Car.Setup != null && v.Track.Name != null && v.Car.Name != null) {
                            this.InitInputTyrePresPredictorDry(v.Track.Name, v.Car.Name, v.Car.Setup.advancedSetup.aeroBalance.brakeDuct, v.Db);
                            for (int i = 0; i < 4; i++) {
                                this.PredictedIdealInputPresDry[i] = double.NaN;
                            }
                        }

                    }
                }

                if (!this._updatingPresPredictorNowWet && rawDataNew.Graphics.rainIntensity != ACC_RAIN_INTENSITY.ACC_NO_RAIN) {
                    if (rawDataNew.Graphics.rainIntensity != rawDataOld.Graphics.rainIntensity || this.InputTyrePresPredictorNowWet == null) {
                        if (v.Car.Setup != null && v.Track.Name != null && v.Car.Name != null) {
                            this.InitInputTyrePresPredictorNowWet(v.Track.Name, v.Car.Name, v.Car.Setup.advancedSetup.aeroBalance.brakeDuct, data, v.Db);
                        }
                        for (int i = 0; i < 4; i++) {
                            this.PredictedIdealInputPresNowWet[i] = double.NaN;
                        }
                    } else if (this._tyreInfo.IdealPres != null) {
                        var preds = this.InputTyrePresPredictorNowWet.Predict(v.Weather.AirTemp, v.Weather.TrackTemp, this._tyreInfo.IdealPres.F, this._tyreInfo.IdealPres.R);
                        preds.CopyTo(this.PredictedIdealInputPresNowWet, 0);
                    }
                }

                if (!this._updatingPresPredictorFutureWet && (rawDataNew.Graphics.rainIntensityIn30min != ACC_RAIN_INTENSITY.ACC_NO_RAIN || rawDataNew.Graphics.rainIntensityIn10min != ACC_RAIN_INTENSITY.ACC_NO_RAIN)) {
                    if (rawDataNew.Graphics.rainIntensityIn30min != rawDataOld.Graphics.rainIntensityIn30min || this.InputTyrePresPredictorFutureWet == null) {
                        if (v.Car.Setup != null && v.Track.Name != null && v.Car.Name != null) {
                            this.InitInputTyrePresPredictorFutureWet(v.Track.Name, v.Car.Name, v.Car.Setup.advancedSetup.aeroBalance.brakeDuct, data, v.Db);
                        }
                        for (int i = 0; i < 4; i++) {
                            this.PredictedIdealInputPresFutureWet[i] = double.NaN;
                        }
                    } else if (this._tyreInfo.IdealPres != null) {
                        var preds = this.InputTyrePresPredictorFutureWet.Predict(v.Weather.AirTemp, v.Weather.TrackTemp, this._tyreInfo.IdealPres.F, this._tyreInfo.IdealPres.R);
                        preds.CopyTo(this.PredictedIdealInputPresFutureWet, 0);
                    }
                }
            }
        }


        private void InitInputTyrePresPredictorDry(string trackName, string carName, int[] brakeDucts, Database.Database db) {
            this.InputTyrePresPredictorDry = null;
            _ = Task.Run(() => {
                this._updatingPresPredictorDry = true;
                this.InputTyrePresPredictorDry = new InputTyrePresPredictor(trackName, carName, brakeDucts, "dry_compound", ACC_RAIN_INTENSITY.ACC_NO_RAIN, $"(0, 1, 2)", db);
                this._updatingPresPredictorDry = false;
            });
            RaceEngineerPlugin.LogInfo("Started building dry tyre pres models.");
        }

        private void InitInputTyrePresPredictorNowWet(string trackName, string carName, int[] brakeDucts, GameData data, Database.Database db) {
            this.InputTyrePresPredictorNowWet = null;
            if (RaceEngineerPlugin.Game.IsAcc) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();
                _ = Task.Run(() => {
                    this._updatingPresPredictorNowWet = true;
                    this.InputTyrePresPredictorNowWet = new InputTyrePresPredictor(trackName, carName, brakeDucts, "wet_compound", rawDataNew.Graphics.rainIntensity, $"({(int)rawDataNew.Graphics.trackGripStatus})", db);
                    this._updatingPresPredictorNowWet = false;
                });
                RaceEngineerPlugin.LogInfo("Started building now wet tyre pres models.");
            }
        }

        private void InitInputTyrePresPredictorFutureWet(string trackName, string carName, int[] brakeDucts, GameData data, Database.Database db) {
            this.InputTyrePresPredictorFutureWet = null;

            if (RaceEngineerPlugin.Game.IsAcc) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();

                _ = Task.Run(() => {
                    this._updatingPresPredictorFutureWet = true;

                    var futureTrackGrip = ACC_TRACK_GRIP_STATUS.ACC_WET;
                    if (rawDataNew.Graphics.rainIntensityIn30min == ACC_RAIN_INTENSITY.ACC_THUNDERSTORM) {
                        futureTrackGrip = ACC_TRACK_GRIP_STATUS.ACC_FLOODED;
                    } else if (rawDataNew.Graphics.rainIntensityIn30min == ACC_RAIN_INTENSITY.ACC_DRIZZLE
                        && (rawDataNew.Graphics.rainIntensity == ACC_RAIN_INTENSITY.ACC_NO_RAIN
                            || rawDataNew.Graphics.rainIntensity == ACC_RAIN_INTENSITY.ACC_DRIZZLE
                            || rawDataNew.Graphics.rainIntensity == ACC_RAIN_INTENSITY.ACC_LIGHT_RAIN
                        )
                    ) {
                        futureTrackGrip = ACC_TRACK_GRIP_STATUS.ACC_DAMP;
                    }

                    this.InputTyrePresPredictorFutureWet = new InputTyrePresPredictor(trackName, carName, brakeDucts, "wet_compound", rawDataNew.Graphics.rainIntensityIn30min, $"({(int)futureTrackGrip})", db);
                    this._updatingPresPredictorFutureWet = false;
                });
                RaceEngineerPlugin.LogInfo("Started building future wet tyre pres models.");
            }
        }


        private void ResetColors() {
            RaceEngineerPlugin.LogInfo("Tyres.ResetColors()");
            this.PresColorF = new Color.ColorCalculator(RaceEngineerPlugin.Settings.PresColor, RaceEngineerPlugin.Settings.TyrePresColorDefValues);
            this.PresColorR = new Color.ColorCalculator(RaceEngineerPlugin.Settings.PresColor, RaceEngineerPlugin.Settings.TyrePresColorDefValues);
            this.TempColorF = new Color.ColorCalculator(RaceEngineerPlugin.Settings.TempColor, RaceEngineerPlugin.Settings.TyreTempColorDefValues);
            this.TempColorR = new Color.ColorCalculator(RaceEngineerPlugin.Settings.TempColor, RaceEngineerPlugin.Settings.TyreTempColorDefValues);
        }

        private void ResetValues() {
            RaceEngineerPlugin.LogInfo("Tyres.ResetValues()");
            for (var i = 0; i < 4; i++) {
                this.IdealInputPres[i] = double.NaN;
            }
            this.PresOverLap.Reset();
            this.TempOverLap.Reset();
            this._presRunning.Reset();
            this._tempRunning.Reset();
            this.InputTyrePresPredictorDry = null;
            this.InputTyrePresPredictorFutureWet = null;
            this.InputTyrePresPredictorNowWet = null;
            //tyreInfo = null;
        }

        #endregion

    }


    public class InputTyrePresPredictor {
        private ML.RidgeRegression?[] regressors;
        private string trackName;
        private string carName;
        private int[] brakeDucts;
        private string compound;

        public InputTyrePresPredictor(string trackName, string carName, int[] brakeDucts, string compound, ACC_RAIN_INTENSITY rain_intensity, string trackGrip, Database.Database db) {
            this.trackName = trackName;
            this.carName = carName;
            this.brakeDucts = brakeDucts;
            this.compound = compound;

            this.regressors = new ML.RidgeRegression?[] {
                this.InitRegressor(0, rain_intensity, trackGrip, db),
                this.InitRegressor(1, rain_intensity, trackGrip, db),
                this.InitRegressor(2, rain_intensity, trackGrip, db),
                this.InitRegressor(3, rain_intensity, trackGrip, db)
            };

            RaceEngineerPlugin.LogInfo($"Created InputTyrePresPredictor({trackName}, {carName}, [{brakeDucts[0]}, {brakeDucts[1]}], {compound})");
        }

        private ML.RidgeRegression? InitRegressor(int tyre, ACC_RAIN_INTENSITY rainIntensity, string trackGrip, Database.Database db) {
            var data = db.GetInputPresData(tyre, this.carName, this.trackName, tyre < 2 ? this.brakeDucts[0] : this.brakeDucts[1], this.compound, trackGrip, rainIntensity);
            if (data.Item2.Count != 0) {
                return new ML.RidgeRegression(data.Item1, data.Item2);
            } else {
                return null;
            }
        }

        public double[] Predict(double airtemp, double tracktemp, double idealPresFront, double idealPresRear) {
            var res = new double[4];
            for (int i = 0; i < 4; i++) {
                if (this.regressors[i] != null && airtemp != 0.0) {
                    res[i] = this.regressors[i]?.Predict(new double[] { i < 3 ? idealPresFront : idealPresRear, airtemp, tracktemp }) ?? -1.0;
                } else {
                    res[i] = double.NaN;
                }
            }
            return res;
        }

    }

}