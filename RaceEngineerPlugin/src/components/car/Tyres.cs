using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using ACSharedMemory.ACC.MMFModels;

using GameReaderCommon;

using KLPlugins.RaceEngineer.Interpolator;
using KLPlugins.RaceEngineer.Stats;

namespace KLPlugins.RaceEngineer.Car {

    public class Tyres {
        public static readonly ImmutableWheelsData<string> Names = new("11", "12", "21", "22");

        public string? Name { get; private set; }

        public WheelsData<double> IdealInputPres { get; }
        public WheelsData<double> PredictedIdealInputPresDry { get; }
        public WheelsData<double> PredictedIdealInputPresNowWet { get; }
        public WheelsData<double> PredictedIdealInputPresFutureWet { get; }
        public WheelsData<double> CurrentInputPres { get; }
        public WheelsData<double> PressDeltaToIdeal { get; }
        public WheelsData<double> PresLoss { get; }
        public WheelsData<bool> PresLossLap { get; }

        private MultiPointLinearInterpolator _tempNormalizerF { get; set; }
        private MultiPointLinearInterpolator _tempNormalizerR { get; set; }

        private const double NORMALIZED_DATA_DEF_VALUE = -1.0;
        public WheelsData<double> TempNormalized { get; } = new(NORMALIZED_DATA_DEF_VALUE);
        public WheelsData<double> TempMinNormalized { get; } = new(NORMALIZED_DATA_DEF_VALUE);
        public WheelsData<double> TempMaxNormalized { get; } = new(NORMALIZED_DATA_DEF_VALUE);
        public WheelsData<double> TempAvgNormalized { get; } = new(NORMALIZED_DATA_DEF_VALUE);

        public WheelsData<double> TempInnerAvgNormalized { get; } = new(NORMALIZED_DATA_DEF_VALUE);
        public WheelsData<double> TempMiddleAvgNormalized { get; } = new(NORMALIZED_DATA_DEF_VALUE);
        public WheelsData<double> TempOuterAvgNormalized { get; } = new(NORMALIZED_DATA_DEF_VALUE);

        private MultiPointLinearInterpolator _presNormalizerF { get; set; }
        private MultiPointLinearInterpolator _presNormalizerR { get; set; }
        public WheelsData<double> PresNormalized { get; } = new(NORMALIZED_DATA_DEF_VALUE);
        public WheelsData<double> PresMinNormalized { get; } = new(NORMALIZED_DATA_DEF_VALUE);
        public WheelsData<double> PresMaxNormalized { get; } = new(NORMALIZED_DATA_DEF_VALUE);
        public WheelsData<double> PresAvgNormalized { get; } = new(NORMALIZED_DATA_DEF_VALUE);

        public int CurrentTyreSet { get; private set; }

        public WheelsStats PresOverLap { get; }
        public WheelsStats TempOverLap { get; }
        public WheelsStats TempInnerOverLap { get; }
        public WheelsStats TempMiddleOverLap { get; }
        public WheelsStats TempOuterOverLap { get; }


        private Dictionary<string, Dictionary<int, int>> _setLaps { get; set; }

        public InputTyrePresPredictor? InputTyrePresPredictorDry { get; private set; }
        public InputTyrePresPredictor? InputTyrePresPredictorNowWet { get; private set; }
        public InputTyrePresPredictor? InputTyrePresPredictorFutureWet { get; private set; }

        public TyreInfo Info { get; private set; } = TyreInfo.Default();

        private volatile bool _updatingPresPredictorDry = false;
        private volatile bool _updatingPresPredictorNowWet = false;
        private volatile bool _updatingPresPredictorFutureWet = false;
        private readonly WheelsRunningStats _presRunning = new();
        private readonly WheelsRunningStats _tempRunning = new();
        private readonly WheelsRunningStats _tempInnerRunning = new();
        private readonly WheelsRunningStats _tempMiddleRunning = new();
        private readonly WheelsRunningStats _tempOuterRunning = new();
        private double _lastSampleTimeSec = DateTime.Now.Second;
        private int _wetSet = 0;

        public Tyres() {
            RaceEngineerPlugin.LogInfo("Created new Tyres");
            this.PresOverLap = new WheelsStats();
            this.TempOverLap = new WheelsStats();
            this.TempInnerOverLap = new WheelsStats();
            this.TempMiddleOverLap = new WheelsStats();
            this.TempOuterOverLap = new WheelsStats();
            this.IdealInputPres = new(double.NaN);
            this.PredictedIdealInputPresDry = new(double.NaN);
            this.PredictedIdealInputPresNowWet = new(double.NaN);
            this.PredictedIdealInputPresFutureWet = new(double.NaN);
            this.CurrentInputPres = new(double.NaN);
            this.PressDeltaToIdeal = new(double.NaN);
            this.PresLoss = new(0.0);
            this.PresLossLap = new(false);
            this._setLaps = [];

            this._tempNormalizerF = DefTempNormalizer();
            this._tempNormalizerR = DefTempNormalizer();

            this._presNormalizerF = DefPresNormalizer();
            this._presNormalizerR = DefPresNormalizer();


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
                this.PressDeltaToIdeal[i] = double.NaN;
                this.PresLoss[i] = 0.0;
                this.PresLossLap[i] = false;
            }

            this.PresOverLap.Reset();
            this.TempOverLap.Reset();
            this.TempInnerOverLap.Reset();
            this.TempMiddleOverLap.Reset();
            this.TempOuterOverLap.Reset();
            this.TempInnerAvgNormalized.Reset();
            this.TempMiddleAvgNormalized.Reset();
            this.TempOuterAvgNormalized.Reset();

            this._setLaps.Clear();
            this.InputTyrePresPredictorDry = null;
            this.InputTyrePresPredictorNowWet = null;
            this.InputTyrePresPredictorFutureWet = null;

            this._tempNormalizerF = DefTempNormalizer();
            this._tempNormalizerR = DefTempNormalizer();
            this.TempNormalized.Reset();
            this.TempMinNormalized.Reset();
            this.TempMaxNormalized.Reset();
            this.TempAvgNormalized.Reset();

            this._presNormalizerF = DefPresNormalizer();
            this._presNormalizerR = DefPresNormalizer();
            this.PresNormalized.Reset();
            this.PresMinNormalized.Reset();
            this.PresAvgNormalized.Reset();
            this.PresMaxNormalized.Reset();

            this._updatingPresPredictorDry = false;
            this._updatingPresPredictorNowWet = false;
            this._updatingPresPredictorFutureWet = false;
            this._presRunning.Reset();
            this._tempRunning.Reset();
            this._tempInnerRunning.Reset();
            this._tempMiddleRunning.Reset();
            this._tempOuterRunning.Reset();
            this.Info = TyreInfo.Default();
            this._wetSet = 0;
            this.CurrentTyreSet = 0;
        }

        public int GetCurrentSetLaps() {
            if (this.Name == null) return -1;

            if (this._setLaps.ContainsKey(this.Name) && this._setLaps[this.Name].ContainsKey(this.CurrentTyreSet)) {
                return this._setLaps[this.Name][this.CurrentTyreSet];
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
            if (this.Name == null) return;

            if (!this._setLaps[this.Name].ContainsKey(this.CurrentTyreSet)) {
                this._setLaps[this.Name][this.CurrentTyreSet] = 0;
            }
        }

        public void OnLapFinished(Values v) {
            if (this.Name != null) {
                if (!this._setLaps[this.Name].ContainsKey(this.CurrentTyreSet)) {
                    this._setLaps[this.Name][this.CurrentTyreSet] = 0;
                }

                this._setLaps[this.Name][this.CurrentTyreSet] += 1;

            }

            this.PresOverLap.Update(this._presRunning);
            this.TempOverLap.Update(this._tempRunning);
            this.TempInnerOverLap.Update(this._tempInnerRunning);
            this.TempMiddleOverLap.Update(this._tempMiddleRunning);
            this.TempOuterOverLap.Update(this._tempOuterRunning);


            for (int iFront = 0; iFront < 2; iFront++) {
                var iRear = iFront + 2;
                this.PressDeltaToIdeal[iFront] = this.PresOverLap[iFront].Avg - this.Info.IdealPresRange.F.Avg;
                this.PressDeltaToIdeal[iRear] = this.PresOverLap[iRear].Avg - this.Info.IdealPresRange.R.Avg;
            }


            this.UpdateIdealInputPressures(v.Weather.AirTemp, v.Weather.TrackTemp);
            this.UpdateOverLapNormalizedData(v);
            this._presRunning.Reset();
            this._tempRunning.Reset();
            this._tempInnerRunning.Reset();
            this._tempMiddleRunning.Reset();
            this._tempOuterRunning.Reset();


        }

        public void OnLapFinishedAfterInsert() {
            for (int i = 0; i < 4; i++) {
                this.PresLossLap[i] = false;
            }
        }

        public void OnRegularUpdate(GameData data, Values v) {
            this.CheckCompoundChange(data, v, data.NewData.TrackId);
            this.CheckPresChange(data, v.Booleans);
            this.UpdateOverLapData(data, v.Booleans);
            this.PredictIdealInputPressures(data, v);
            this.UpdateNormalizedData(data, v.Booleans.NewData.IsInMenu);
        }

        #endregion

        #region PRIVATE METHODS

        private void UpdateNormalizedData(GameData data, bool isInMenu) {
            if (!isInMenu) {
                if ((WheelFlags.Color & RaceEngineerPlugin.Settings.TyrePresFlags) != 0) {
                    this.PresNormalized.FL = this._presNormalizerF.Interpolate(data.NewData.TyrePressureFrontLeft);
                    this.PresNormalized.FR = this._presNormalizerF.Interpolate(data.NewData.TyrePressureFrontRight);
                    this.PresNormalized.RL = this._presNormalizerR.Interpolate(data.NewData.TyrePressureRearLeft);
                    this.PresNormalized.RR = this._presNormalizerR.Interpolate(data.NewData.TyrePressureRearRight);
                }

                if ((WheelFlags.Color & RaceEngineerPlugin.Settings.TyreTempFlags) != 0) {
                    this.TempNormalized.FL = this._tempNormalizerF.Interpolate(data.NewData.TyreTemperatureFrontLeft);
                    this.TempNormalized.FR = this._tempNormalizerF.Interpolate(data.NewData.TyreTemperatureFrontRight);
                    this.TempNormalized.RL = this._tempNormalizerR.Interpolate(data.NewData.TyreTemperatureRearLeft);
                    this.TempNormalized.RR = this._tempNormalizerR.Interpolate(data.NewData.TyreTemperatureRearRight);
                }
            }
        }

        private void UpdateOverLapNormalizedData(Values v) {
            if ((WheelFlags.MinColor & RaceEngineerPlugin.Settings.TyrePresFlags) != 0) {
                this.PresMinNormalized.FL = this._presNormalizerF.Interpolate(this.PresOverLap.FL.Min);
                this.PresMinNormalized.FR = this._presNormalizerF.Interpolate(this.PresOverLap.FR.Min);
                this.PresMinNormalized.RL = this._presNormalizerR.Interpolate(this.PresOverLap.RL.Min);
                this.PresMinNormalized.RR = this._presNormalizerR.Interpolate(this.PresOverLap.RR.Min);
            }
            if ((WheelFlags.MaxColor & RaceEngineerPlugin.Settings.TyrePresFlags) != 0) {
                this.PresMaxNormalized.FL = this._presNormalizerF.Interpolate(this.PresOverLap.FL.Max);
                this.PresMaxNormalized.FR = this._presNormalizerF.Interpolate(this.PresOverLap.FR.Max);
                this.PresMaxNormalized.RL = this._presNormalizerR.Interpolate(this.PresOverLap.RL.Max);
                this.PresMaxNormalized.RR = this._presNormalizerR.Interpolate(this.PresOverLap.RR.Max);
            }
            if ((WheelFlags.AvgColor & RaceEngineerPlugin.Settings.TyrePresFlags) != 0) {
                this.PresAvgNormalized.FL = this._presNormalizerF.Interpolate(this.PresOverLap.FL.Avg);
                this.PresAvgNormalized.FR = this._presNormalizerF.Interpolate(this.PresOverLap.FR.Avg);
                this.PresAvgNormalized.RL = this._presNormalizerR.Interpolate(this.PresOverLap.RL.Avg);
                this.PresAvgNormalized.RR = this._presNormalizerR.Interpolate(this.PresOverLap.RR.Avg);
            }

            if ((WheelFlags.MinColor & RaceEngineerPlugin.Settings.TyreTempFlags) != 0) {
                this.TempMinNormalized.FL = this._tempNormalizerF.Interpolate(this.TempOverLap.FL.Min);
                this.TempMinNormalized.FR = this._tempNormalizerF.Interpolate(this.TempOverLap.FR.Min);
                this.TempMinNormalized.RL = this._tempNormalizerR.Interpolate(this.TempOverLap.RL.Min);
                this.TempMinNormalized.RR = this._tempNormalizerR.Interpolate(this.TempOverLap.RR.Min);
            }
            if ((WheelFlags.MaxColor & RaceEngineerPlugin.Settings.TyreTempFlags) != 0) {
                this.TempMaxNormalized.FL = this._tempNormalizerF.Interpolate(this.TempOverLap.FL.Max);
                this.TempMaxNormalized.FR = this._tempNormalizerF.Interpolate(this.TempOverLap.FR.Max);
                this.TempMaxNormalized.RL = this._tempNormalizerR.Interpolate(this.TempOverLap.RL.Max);
                this.TempMaxNormalized.RR = this._tempNormalizerR.Interpolate(this.TempOverLap.RR.Max);
            }
            if ((WheelFlags.AvgColor & RaceEngineerPlugin.Settings.TyreTempFlags) != 0) {
                this.TempAvgNormalized.FL = this._tempNormalizerF.Interpolate(this.TempOverLap.FL.Avg);
                this.TempAvgNormalized.FR = this._tempNormalizerF.Interpolate(this.TempOverLap.FR.Avg);
                this.TempAvgNormalized.RL = this._tempNormalizerR.Interpolate(this.TempOverLap.RL.Avg);
                this.TempAvgNormalized.RR = this._tempNormalizerR.Interpolate(this.TempOverLap.RR.Avg);

                this.TempInnerAvgNormalized.FL = this._tempNormalizerF.Interpolate(this.TempInnerOverLap.FL.Avg);
                this.TempInnerAvgNormalized.FR = this._tempNormalizerF.Interpolate(this.TempInnerOverLap.FR.Avg);
                this.TempInnerAvgNormalized.RL = this._tempNormalizerR.Interpolate(this.TempInnerOverLap.RL.Avg);
                this.TempInnerAvgNormalized.RR = this._tempNormalizerR.Interpolate(this.TempInnerOverLap.RR.Avg);

                this.TempMiddleAvgNormalized.FL = this._tempNormalizerF.Interpolate(this.TempMiddleOverLap.FL.Avg);
                this.TempMiddleAvgNormalized.FR = this._tempNormalizerF.Interpolate(this.TempMiddleOverLap.FR.Avg);
                this.TempMiddleAvgNormalized.RL = this._tempNormalizerR.Interpolate(this.TempMiddleOverLap.RL.Avg);
                this.TempMiddleAvgNormalized.RR = this._tempNormalizerR.Interpolate(this.TempMiddleOverLap.RR.Avg);

                this.TempOuterAvgNormalized.FL = this._tempNormalizerF.Interpolate(this.TempOuterOverLap.FL.Avg);
                this.TempOuterAvgNormalized.FR = this._tempNormalizerF.Interpolate(this.TempOuterOverLap.FR.Avg);
                this.TempOuterAvgNormalized.RL = this._tempNormalizerR.Interpolate(this.TempOuterOverLap.RL.Avg);
                this.TempOuterAvgNormalized.RR = this._tempNormalizerR.Interpolate(this.TempOuterOverLap.RR.Avg);
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
            } else if (RaceEngineerPlugin.Game.IsAc) {
                var rawDataNew = (ACSharedMemory.Reader.ACRawData)data.NewData.GetRawDataObject();
                newTyreName = rawDataNew.Graphics.TyreCompound;
                // we assume every pit stop changes a tyre set too, not necessarily true
                this.CurrentTyreSet += 1;
            } else {
                // TODO: figure out other games
                newTyreName = "unknown";
                this.CurrentTyreSet += 1;
            }

            if (newTyreName == null || newTyreName == this.Name) return;
            RaceEngineerPlugin.LogInfo($"Tyres changed from '{this.Name}' to '{newTyreName}'.");
            this.ResetValues();

            this.Name = newTyreName;
            if (!this._setLaps.ContainsKey(this.Name)) {
                this._setLaps[this.Name] = [];
            }

            if (v.Car.Info.Tyres.ContainsKey(this.Name)) {
                RaceEngineerPlugin.LogInfo($"Tyre info found for '{this.Name}'.");
                this.Info = v.Car.Info.Tyres[this.Name];
            } else if (v.Car.Info.Tyres.ContainsKey("def")) {
                RaceEngineerPlugin.LogInfo($"Tyre info not found for '{this.Name}'. Using `def` values.");
                this.Info = v.Car.Info.Tyres["def"];
            } else {
                RaceEngineerPlugin.LogInfo($"Tyre info not found for '{this.Name}'. Using `TyreInfo.Default()`.");
                this.Info = TyreInfo.Default();
            }

            this._presNormalizerF = new(this.Info.IdealPresCurve.F);
            this._presNormalizerR = new(this.Info.IdealPresCurve.R);
            this._tempNormalizerF = new(this.Info.IdealTempCurve.F);
            this._tempNormalizerR = new(this.Info.IdealTempCurve.R);
        }

        static MultiPointLinearInterpolator DefPresNormalizer() {
            return new(RaceEngineerPlugin.Settings.TyrePresNormalizationLut);
        }

        static MultiPointLinearInterpolator DefTempNormalizer() {
            return new(RaceEngineerPlugin.Settings.TyreTempNormalizationLut);
        }


        private void ResetPressureLoss() {
            for (int i = 0; i < 4; i++) {
                this.PresLoss[i] = 0.0;
            }
        }

        private void CheckPresChange(GameData data, Booleans.Booleans booleans) {
            if (booleans.NewData.IsInMenu) {
                return;
            }

            double press_loss_threshold;
            if (RaceEngineerPlugin.Game.IsAcc || RaceEngineerPlugin.Game.IsAc) {
                press_loss_threshold = 0.1;
            } else if (RaceEngineerPlugin.Game.IsRf2) {
                // in rf2 the tyre pressures jump around a lot when one goes off road
                press_loss_threshold = 1.0;
            } else {
                // be careful for other games as well
                press_loss_threshold = 1.0;
            }

            var presDelta = new double[4] {
                 data.NewData.TyrePressureFrontLeft - data.OldData.TyrePressureFrontLeft,
                 data.NewData.TyrePressureFrontRight - data.OldData.TyrePressureFrontRight,
                 data.NewData.TyrePressureRearLeft - data.OldData.TyrePressureRearLeft,
                 data.NewData.TyrePressureRearRight - data.OldData.TyrePressureRearRight
            };

            if (data.NewData.SpeedKmh > 10) {
                for (int i = 0; i < 4; i++) {
                    if (presDelta[i] < -press_loss_threshold) {
                        this.PresLoss[i] += presDelta[i];
                        this.PresLossLap[i] = true;
                        RaceEngineerPlugin.LogInfo($"Pressure loss on {Names[i]} by {presDelta[i]}.");
                    }
                }
            } else {
                var areInputPressuresNotSet = double.IsNaN(this.CurrentInputPres[0]);
                // If tyre pressure changed suddenly while not moving, it's probably a tyre change and we need to take new input pressures
                static bool pred(double v) => Math.Abs(v) > 0.1;
                if (areInputPressuresNotSet || pred(presDelta[0]) || pred(presDelta[1]) || pred(presDelta[2]) || pred(presDelta[3])) {
                    this.CurrentInputPres[0] = Math.Ceiling(data.NewData.TyrePressureFrontLeft * 10.0) / 10.0;
                    this.CurrentInputPres[1] = Math.Ceiling(data.NewData.TyrePressureFrontRight * 10.0) / 10.0;
                    this.CurrentInputPres[2] = Math.Ceiling(data.NewData.TyrePressureRearLeft * 10.0) / 10.0;
                    this.CurrentInputPres[3] = Math.Ceiling(data.NewData.TyrePressureRearRight * 10.0) / 10.0;

                    RaceEngineerPlugin.LogInfo($"Current input tyre pressures updated to [{this.CurrentInputPres[0]}, {this.CurrentInputPres[1]}, {this.CurrentInputPres[2]}, {this.CurrentInputPres[3]}].");
                    this.ResetPressureLoss();
                }
            }
        }

        private void UpdateOverLapData(GameData data, Booleans.Booleans booleans) {
            // Add sample to counters
            if (booleans.NewData.IsMoving && booleans.NewData.IsOnTrack) {
                double now = data.FrameTime.Second;
                if (this._lastSampleTimeSec == now) return;
                double[] currentPres = [
                    data.NewData.TyrePressureFrontLeft,
                    data.NewData.TyrePressureFrontRight,
                    data.NewData.TyrePressureRearLeft,
                    data.NewData.TyrePressureRearRight
                ];
                double[] currentTemp = [
                    data.NewData.TyreTemperatureFrontLeft,
                    data.NewData.TyreTemperatureFrontRight,
                    data.NewData.TyreTemperatureRearLeft,
                    data.NewData.TyreTemperatureRearRight
                ];

                this._presRunning.Update(currentPres);
                this._tempRunning.Update(currentTemp);

                double[] currentInnerTemp = [
                    data.NewData.TyreTemperatureFrontLeftInner,
                    data.NewData.TyreTemperatureFrontRightInner,
                    data.NewData.TyreTemperatureRearLeftInner,
                    data.NewData.TyreTemperatureRearRightInner
                ];
                this._tempInnerRunning.Update(currentInnerTemp);

                double[] currentMiddleTemp = [
                    data.NewData.TyreTemperatureFrontLeftMiddle,
                    data.NewData.TyreTemperatureFrontRightMiddle,
                    data.NewData.TyreTemperatureRearLeftMiddle,
                    data.NewData.TyreTemperatureRearRightMiddle
                ];
                this._tempMiddleRunning.Update(currentMiddleTemp);

                double[] currentOuterTemp = [
                    data.NewData.TyreTemperatureFrontLeftOuter,
                    data.NewData.TyreTemperatureFrontRightOuter,
                    data.NewData.TyreTemperatureRearLeftOuter,
                    data.NewData.TyreTemperatureRearRightOuter
                ];
                this._tempOuterRunning.Update(currentOuterTemp);

                this._lastSampleTimeSec = now;
            }
        }

        private void UpdateIdealInputPressures(double airtemp, double tracktemp) {
            for (int iFront = 0; iFront < 2; iFront++) {
                var iRear = iFront + 2;
                this.IdealInputPres[iFront] = this.CurrentInputPres[iFront] + (this.Info.IdealPresRange.F.Avg - this.PresOverLap[iFront].Avg);
                this.IdealInputPres[iRear] = this.CurrentInputPres[iRear] + (this.Info.IdealPresRange.R.Avg - this.PresOverLap[iRear].Avg);
            }
        }

        private void PredictIdealInputPressures(GameData data, Values v) {
            if (RaceEngineerPlugin.Game.IsAcc) {
                if (v.Weather.AirTemp == 0.0) {
                    // RaceEngineerPlugin.LogInfo($"Couldn't update ideal tyre pressures as 'tyreInfo == null' || 'AirTemp == 0.0'");
                    for (int i = 0; i < 4; i++) {
                        this.PredictedIdealInputPresDry[i] = double.NaN;
                        this.PredictedIdealInputPresNowWet[i] = double.NaN;
                        this.PredictedIdealInputPresFutureWet[i] = double.NaN;
                    }
                    return;
                }

                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();
                var rawDataOld = (ACSharedMemory.ACC.Reader.ACCRawData)data.OldData.GetRawDataObject();

                if (!this._updatingPresPredictorDry
                    && (rawDataNew.Graphics.rainIntensity == ACC_RAIN_INTENSITY.ACC_NO_RAIN
                        || rawDataNew.Graphics.rainIntensityIn10min == ACC_RAIN_INTENSITY.ACC_NO_RAIN
                        || rawDataNew.Graphics.rainIntensityIn30min == ACC_RAIN_INTENSITY.ACC_NO_RAIN
                    )
                ) {
                    if (this.InputTyrePresPredictorDry != null) {
                        var preds = this.InputTyrePresPredictorDry.Predict(
                            v.Weather.AirTemp,
                            v.Weather.TrackTemp,
                            this.Info.IdealPresRange.F.Avg,
                            this.Info.IdealPresRange.R.Avg
                        );
                        preds.CopyTo(this.PredictedIdealInputPresDry, 0);
                    } else {
                        if (v.Car.Setup != null && v.Track.Name != null && v.Car.Name != null) {
                            this.InitInputTyrePresPredictorDry(v.Track.Name, v.Car.Name, v.Car.Setup.advancedSetup.aeroBalance.brakeDuct.ToArray(), v.Db);
                            for (int i = 0; i < 4; i++) {
                                this.PredictedIdealInputPresDry[i] = double.NaN;
                            }
                        }

                    }
                }

                if (!this._updatingPresPredictorNowWet && rawDataNew.Graphics.rainIntensity != ACC_RAIN_INTENSITY.ACC_NO_RAIN) {
                    if (rawDataNew.Graphics.rainIntensity != rawDataOld.Graphics.rainIntensity || this.InputTyrePresPredictorNowWet == null) {
                        if (v.Car.Setup != null && v.Track.Name != null && v.Car.Name != null) {
                            this.InitInputTyrePresPredictorNowWet(v.Track.Name, v.Car.Name, v.Car.Setup.advancedSetup.aeroBalance.brakeDuct.ToArray(), data, v.Db);
                        }
                        for (int i = 0; i < 4; i++) {
                            this.PredictedIdealInputPresNowWet[i] = double.NaN;
                        }
                    } else {
                        var preds = this.InputTyrePresPredictorNowWet.Predict(
                            v.Weather.AirTemp,
                            v.Weather.TrackTemp,
                            this.Info.IdealPresRange.F.Avg,
                            this.Info.IdealPresRange.R.Avg
                        );
                        preds.CopyTo(this.PredictedIdealInputPresNowWet, 0);
                    }
                }

                if (!this._updatingPresPredictorFutureWet && (rawDataNew.Graphics.rainIntensityIn30min != ACC_RAIN_INTENSITY.ACC_NO_RAIN || rawDataNew.Graphics.rainIntensityIn10min != ACC_RAIN_INTENSITY.ACC_NO_RAIN)) {
                    if (rawDataNew.Graphics.rainIntensityIn30min != rawDataOld.Graphics.rainIntensityIn30min || this.InputTyrePresPredictorFutureWet == null) {
                        if (v.Car.Setup != null && v.Track.Name != null && v.Car.Name != null) {
                            this.InitInputTyrePresPredictorFutureWet(v.Track.Name, v.Car.Name, v.Car.Setup.advancedSetup.aeroBalance.brakeDuct.ToArray(), data, v.Db);
                        }
                        for (int i = 0; i < 4; i++) {
                            this.PredictedIdealInputPresFutureWet[i] = double.NaN;
                        }
                    } else {
                        var preds = this.InputTyrePresPredictorFutureWet.Predict(
                            v.Weather.AirTemp,
                            v.Weather.TrackTemp,
                            this.Info.IdealPresRange.F.Avg,
                            this.Info.IdealPresRange.R.Avg
                        );
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


        private void ResetNormalizers() {
            RaceEngineerPlugin.LogInfo("Tyres.ResetColors()");
            this._tempNormalizerF = new MultiPointLinearInterpolator(RaceEngineerPlugin.Settings.TyreTempNormalizationLut);
            this._tempNormalizerR = new MultiPointLinearInterpolator(RaceEngineerPlugin.Settings.TyreTempNormalizationLut);
            this._presNormalizerF = new MultiPointLinearInterpolator(RaceEngineerPlugin.Settings.TyrePresNormalizationLut);
            this._presNormalizerR = new MultiPointLinearInterpolator(RaceEngineerPlugin.Settings.TyrePresNormalizationLut);
        }

        private void ResetValues() {
            RaceEngineerPlugin.LogInfo("Tyres.ResetValues()");
            for (var i = 0; i < 4; i++) {
                this.IdealInputPres[i] = double.NaN;
                this.PressDeltaToIdeal[i] = double.NaN;
            }
            this.PresOverLap.Reset();
            this.TempOverLap.Reset();
            this.TempInnerOverLap.Reset();
            this.TempMiddleOverLap.Reset();
            this.TempOuterOverLap.Reset();
            this._presRunning.Reset();
            this._tempRunning.Reset();
            this._tempInnerRunning.Reset();
            this._tempMiddleRunning.Reset();
            this._tempOuterRunning.Reset();
            this.InputTyrePresPredictorDry = null;
            this.InputTyrePresPredictorFutureWet = null;
            this.InputTyrePresPredictorNowWet = null;
            //tyreInfo = null;
        }

        #endregion

    }


    public class InputTyrePresPredictor {
        private readonly ML.RidgeRegression?[] _regressors;
        private readonly string _trackName;
        private readonly string _carName;
        private readonly int[] _brakeDucts;
        private readonly string _compound;

        internal InputTyrePresPredictor(
            string trackName,
             string carName,
             int[] brakeDucts,
             string compound,
              ACC_RAIN_INTENSITY rain_intensity,
              string trackGrip, Database.Database db) {
            this._trackName = trackName;
            this._carName = carName;
            this._brakeDucts = brakeDucts;
            this._compound = compound;

            this._regressors = [
                this.InitRegressor(0, rain_intensity, trackGrip, db),
                this.InitRegressor(1, rain_intensity, trackGrip, db),
                this.InitRegressor(2, rain_intensity, trackGrip, db),
                this.InitRegressor(3, rain_intensity, trackGrip, db)
            ];

            RaceEngineerPlugin.LogInfo($"Created InputTyrePresPredictor({trackName}, {carName}, [{brakeDucts[0]}, {brakeDucts[1]}], {compound})");
        }

        private ML.RidgeRegression? InitRegressor(int tyre, ACC_RAIN_INTENSITY rainIntensity, string trackGrip, Database.Database db) {
            var data = db.GetInputPresData(tyre, this._carName, this._trackName, tyre < 2 ? this._brakeDucts[0] : this._brakeDucts[1], this._compound, trackGrip, rainIntensity);
            if (data.Item2.Count != 0) {
                return new ML.RidgeRegression(data.Item1, data.Item2);
            } else {
                return null;
            }
        }

        public WheelsData<double> Predict(double airtemp, double tracktemp, double idealPresFront, double idealPresRear) {
            var res = new WheelsData<double>(0.0);
            for (int i = 0; i < 4; i++) {
                if (this._regressors[i] != null && airtemp != 0.0) {
                    res[i] = this._regressors[i]?.Predict([i < 3 ? idealPresFront : idealPresRear, airtemp, tracktemp]) ?? -1.0;
                } else {
                    res[i] = double.NaN;
                }
            }
            return res;
        }

    }

}