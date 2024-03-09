using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ACSharedMemory.ACC.MMFModels;

using GameReaderCommon;

using KLPlugins.RaceEngineer.Interpolator;
using KLPlugins.RaceEngineer.Stats;

namespace KLPlugins.RaceEngineer.Car {

    public class Tyres {
        public static readonly ImmutableWheelsData<string> Names = new("11", "12", "21", "22");

        public string? Name { get; private set; }

        public ReadonlyWheelsDataView<double> IdealInputPres => this._idealInputPres.AsReadonlyView();
        public ReadonlyWheelsDataView<double> PredictedIdealInputPresDry => this._predictedIdealInputPresDry.AsReadonlyView();
        public ReadonlyWheelsDataView<double> PredictedIdealInputPresNowWet => this._predictedIdealInputPresNowWet.AsReadonlyView();
        public ReadonlyWheelsDataView<double> PredictedIdealInputPresFutureWet => this._predictedIdealInputPresFutureWet.AsReadonlyView();
        public ReadonlyWheelsDataView<double> CurrentInputPres => this._currentInputPres.AsReadonlyView();
        public ReadonlyWheelsDataView<double> PressDeltaToIdeal => this._pressDeltaToIdeal.AsReadonlyView();
        public ReadonlyWheelsDataView<double> PresLoss => this._presLoss.AsReadonlyView();
        public ReadonlyWheelsDataView<bool> PresLossLap => this._presLossLap.AsReadonlyView();
        public ReadonlyWheelsDataView<double> TempNormalized => this._tempNormalized.AsReadonlyView();
        public ReadonlyWheelsDataView<double> TempMinNormalized => this._tempMinNormalized.AsReadonlyView();
        public ReadonlyWheelsDataView<double> TempMaxNormalized => this._tempMaxNormalized.AsReadonlyView();
        public ReadonlyWheelsDataView<double> TempAvgNormalized => this._tempAvgNormalized.AsReadonlyView();
        public ReadonlyWheelsDataView<double> TempInnerAvgNormalized => this._tempInnerAvgNormalized.AsReadonlyView();
        public ReadonlyWheelsDataView<double> TempMiddleAvgNormalized => this._tempMiddleAvgNormalized.AsReadonlyView();
        public ReadonlyWheelsDataView<double> TempOuterAvgNormalized => this._tempOuterAvgNormalized.AsReadonlyView();
        public ReadonlyWheelsDataView<double> PresNormalized => this._presNormalized.AsReadonlyView();
        public ReadonlyWheelsDataView<double> PresMinNormalized => this._presMinNormalized.AsReadonlyView();
        public ReadonlyWheelsDataView<double> PresMaxNormalized => this._presMaxNormalized.AsReadonlyView();
        public ReadonlyWheelsDataView<double> PresAvgNormalized => this._presAvgNormalized.AsReadonlyView();
        public int CurrentTyreSet { get; private set; }
        public ReadonlyWheelsStatsView PresOverLap => this._presOverLap.AsReadonlyView();
        public ReadonlyWheelsStatsView TempOverLap => this._tempOverLap.AsReadonlyView();
        public ReadonlyWheelsStatsView TempInnerOverLap => this._tempInnerOverLap.AsReadonlyView();
        public ReadonlyWheelsStatsView TempMiddleOverLap => this._tempMiddleOverLap.AsReadonlyView();
        public ReadonlyWheelsStatsView TempOuterOverLap => this._tempOuterOverLap.AsReadonlyView();
        public TyreInfo Info { get; private set; } = TyreInfo.Default();


        // NOTE: It's important to never reassign WheelsData values. 
        // The property exports to SimHub rely on the fact that they point to one place always.
        private readonly WheelsData<double> _idealInputPres = new(double.NaN);
        private readonly WheelsData<double> _predictedIdealInputPresDry = new(double.NaN);
        private readonly WheelsData<double> _predictedIdealInputPresNowWet = new(double.NaN);
        private readonly WheelsData<double> _predictedIdealInputPresFutureWet = new(double.NaN);
        private readonly WheelsData<double> _currentInputPres = new(double.NaN);
        private readonly WheelsData<double> _pressDeltaToIdeal = new(double.NaN);
        private readonly WheelsData<double> _presLoss = new(0.0);
        private readonly WheelsData<bool> _presLossLap = new(false);
        private const double NORMALIZED_DATA_DEF_VALUE = -1.0;
        private readonly WheelsData<double> _tempNormalized = new(NORMALIZED_DATA_DEF_VALUE);
        private readonly WheelsData<double> _tempMinNormalized = new(NORMALIZED_DATA_DEF_VALUE);
        private readonly WheelsData<double> _tempMaxNormalized = new(NORMALIZED_DATA_DEF_VALUE);
        private readonly WheelsData<double> _tempAvgNormalized = new(NORMALIZED_DATA_DEF_VALUE);
        private readonly WheelsData<double> _tempInnerAvgNormalized = new(NORMALIZED_DATA_DEF_VALUE);
        private readonly WheelsData<double> _tempMiddleAvgNormalized = new(NORMALIZED_DATA_DEF_VALUE);
        private readonly WheelsData<double> _tempOuterAvgNormalized = new(NORMALIZED_DATA_DEF_VALUE);
        private readonly WheelsData<double> _presNormalized = new(NORMALIZED_DATA_DEF_VALUE);
        private readonly WheelsData<double> _presMinNormalized = new(NORMALIZED_DATA_DEF_VALUE);
        private readonly WheelsData<double> _presMaxNormalized = new(NORMALIZED_DATA_DEF_VALUE);
        private readonly WheelsData<double> _presAvgNormalized = new(NORMALIZED_DATA_DEF_VALUE);
        private readonly WheelsStats _presOverLap = new();
        private readonly WheelsStats _tempOverLap = new();
        private readonly WheelsStats _tempInnerOverLap = new();
        private readonly WheelsStats _tempMiddleOverLap = new();
        private readonly WheelsStats _tempOuterOverLap = new();
        private MultiPointLinearInterpolator _tempNormalizerF = DefTempNormalizer();
        private MultiPointLinearInterpolator _tempNormalizerR = DefTempNormalizer();
        private MultiPointLinearInterpolator _presNormalizerF = DefPresNormalizer();
        private MultiPointLinearInterpolator _presNormalizerR = DefPresNormalizer();
        private readonly Dictionary<string, Dictionary<int, int>> _setLaps = [];
        internal InputTyrePresPredictor? InputTyrePresPredictorDry { get; private set; }
        internal InputTyrePresPredictor? InputTyrePresPredictorNowWet { get; private set; }
        internal InputTyrePresPredictor? InputTyrePresPredictorFutureWet { get; private set; }
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
        }

        public void Reset() {
            this.Name = null;
            this._idealInputPres.Reset();
            this._predictedIdealInputPresDry.Reset();
            this._predictedIdealInputPresNowWet.Reset();
            this._predictedIdealInputPresFutureWet.Reset();
            this._currentInputPres.Reset();
            this._pressDeltaToIdeal.Reset();
            this._presLoss.Reset();
            this._presLossLap.Reset();

            this._presOverLap.Reset();
            this._tempOverLap.Reset();
            this._tempInnerOverLap.Reset();
            this._tempMiddleOverLap.Reset();
            this._tempOuterOverLap.Reset();
            this._tempInnerAvgNormalized.Reset();
            this._tempMiddleAvgNormalized.Reset();
            this._tempOuterAvgNormalized.Reset();

            this._setLaps.Clear();
            this.InputTyrePresPredictorDry = null;
            this.InputTyrePresPredictorNowWet = null;
            this.InputTyrePresPredictorFutureWet = null;

            this._tempNormalizerF = DefTempNormalizer();
            this._tempNormalizerR = DefTempNormalizer();
            this._tempNormalized.Reset();
            this._tempMinNormalized.Reset();
            this._tempMaxNormalized.Reset();
            this._tempAvgNormalized.Reset();

            this._presNormalizerF = DefPresNormalizer();
            this._presNormalizerR = DefPresNormalizer();
            this._presNormalized.Reset();
            this._presMinNormalized.Reset();
            this._presAvgNormalized.Reset();
            this._presMaxNormalized.Reset();

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

            this._presOverLap.Update(this._presRunning);
            this._tempOverLap.Update(this._tempRunning);
            this._tempInnerOverLap.Update(this._tempInnerRunning);
            this._tempMiddleOverLap.Update(this._tempMiddleRunning);
            this._tempOuterOverLap.Update(this._tempOuterRunning);


            for (int iFront = 0; iFront < 2; iFront++) {
                var iRear = iFront + 2;
                this._pressDeltaToIdeal[iFront] = this._presOverLap[iFront].Avg - this.Info.IdealPresRange.F.Avg;
                this._pressDeltaToIdeal[iRear] = this._presOverLap[iRear].Avg - this.Info.IdealPresRange.R.Avg;
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
                this._presLossLap[i] = false;
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
                    this._presNormalized.FL = this._presNormalizerF.Interpolate(data.NewData.TyrePressureFrontLeft);
                    this._presNormalized.FR = this._presNormalizerF.Interpolate(data.NewData.TyrePressureFrontRight);
                    this._presNormalized.RL = this._presNormalizerR.Interpolate(data.NewData.TyrePressureRearLeft);
                    this._presNormalized.RR = this._presNormalizerR.Interpolate(data.NewData.TyrePressureRearRight);
                }

                if ((WheelFlags.Color & RaceEngineerPlugin.Settings.TyreTempFlags) != 0) {
                    this._tempNormalized.FL = this._tempNormalizerF.Interpolate(data.NewData.TyreTemperatureFrontLeft);
                    this._tempNormalized.FR = this._tempNormalizerF.Interpolate(data.NewData.TyreTemperatureFrontRight);
                    this._tempNormalized.RL = this._tempNormalizerR.Interpolate(data.NewData.TyreTemperatureRearLeft);
                    this._tempNormalized.RR = this._tempNormalizerR.Interpolate(data.NewData.TyreTemperatureRearRight);
                }
            }
        }

        private void UpdateOverLapNormalizedData(Values v) {
            if ((WheelFlags.MinColor & RaceEngineerPlugin.Settings.TyrePresFlags) != 0) {
                this._presMinNormalized.FL = this._presNormalizerF.Interpolate(this._presOverLap.FL.Min);
                this._presMinNormalized.FR = this._presNormalizerF.Interpolate(this._presOverLap.FR.Min);
                this._presMinNormalized.RL = this._presNormalizerR.Interpolate(this._presOverLap.RL.Min);
                this._presMinNormalized.RR = this._presNormalizerR.Interpolate(this._presOverLap.RR.Min);
            }
            if ((WheelFlags.MaxColor & RaceEngineerPlugin.Settings.TyrePresFlags) != 0) {
                this._presMaxNormalized.FL = this._presNormalizerF.Interpolate(this._presOverLap.FL.Max);
                this._presMaxNormalized.FR = this._presNormalizerF.Interpolate(this._presOverLap.FR.Max);
                this._presMaxNormalized.RL = this._presNormalizerR.Interpolate(this._presOverLap.RL.Max);
                this._presMaxNormalized.RR = this._presNormalizerR.Interpolate(this._presOverLap.RR.Max);
            }
            if ((WheelFlags.AvgColor & RaceEngineerPlugin.Settings.TyrePresFlags) != 0) {
                this._presAvgNormalized.FL = this._presNormalizerF.Interpolate(this._presOverLap.FL.Avg);
                this._presAvgNormalized.FR = this._presNormalizerF.Interpolate(this._presOverLap.FR.Avg);
                this._presAvgNormalized.RL = this._presNormalizerR.Interpolate(this._presOverLap.RL.Avg);
                this._presAvgNormalized.RR = this._presNormalizerR.Interpolate(this._presOverLap.RR.Avg);
            }

            if ((WheelFlags.MinColor & RaceEngineerPlugin.Settings.TyreTempFlags) != 0) {
                this._tempMinNormalized.FL = this._tempNormalizerF.Interpolate(this._tempOverLap.FL.Min);
                this._tempMinNormalized.FR = this._tempNormalizerF.Interpolate(this._tempOverLap.FR.Min);
                this._tempMinNormalized.RL = this._tempNormalizerR.Interpolate(this._tempOverLap.RL.Min);
                this._tempMinNormalized.RR = this._tempNormalizerR.Interpolate(this._tempOverLap.RR.Min);
            }
            if ((WheelFlags.MaxColor & RaceEngineerPlugin.Settings.TyreTempFlags) != 0) {
                this._tempMaxNormalized.FL = this._tempNormalizerF.Interpolate(this._tempOverLap.FL.Max);
                this._tempMaxNormalized.FR = this._tempNormalizerF.Interpolate(this._tempOverLap.FR.Max);
                this._tempMaxNormalized.RL = this._tempNormalizerR.Interpolate(this._tempOverLap.RL.Max);
                this._tempMaxNormalized.RR = this._tempNormalizerR.Interpolate(this._tempOverLap.RR.Max);
            }
            if ((WheelFlags.AvgColor & RaceEngineerPlugin.Settings.TyreTempFlags) != 0) {
                this._tempAvgNormalized.FL = this._tempNormalizerF.Interpolate(this._tempOverLap.FL.Avg);
                this._tempAvgNormalized.FR = this._tempNormalizerF.Interpolate(this._tempOverLap.FR.Avg);
                this._tempAvgNormalized.RL = this._tempNormalizerR.Interpolate(this._tempOverLap.RL.Avg);
                this._tempAvgNormalized.RR = this._tempNormalizerR.Interpolate(this._tempOverLap.RR.Avg);

                this._tempInnerAvgNormalized.FL = this._tempNormalizerF.Interpolate(this._tempInnerOverLap.FL.Avg);
                this._tempInnerAvgNormalized.FR = this._tempNormalizerF.Interpolate(this._tempInnerOverLap.FR.Avg);
                this._tempInnerAvgNormalized.RL = this._tempNormalizerR.Interpolate(this._tempInnerOverLap.RL.Avg);
                this._tempInnerAvgNormalized.RR = this._tempNormalizerR.Interpolate(this._tempInnerOverLap.RR.Avg);

                this._tempMiddleAvgNormalized.FL = this._tempNormalizerF.Interpolate(this._tempMiddleOverLap.FL.Avg);
                this._tempMiddleAvgNormalized.FR = this._tempNormalizerF.Interpolate(this._tempMiddleOverLap.FR.Avg);
                this._tempMiddleAvgNormalized.RL = this._tempNormalizerR.Interpolate(this._tempMiddleOverLap.RL.Avg);
                this._tempMiddleAvgNormalized.RR = this._tempNormalizerR.Interpolate(this._tempMiddleOverLap.RR.Avg);

                this._tempOuterAvgNormalized.FL = this._tempNormalizerF.Interpolate(this._tempOuterOverLap.FL.Avg);
                this._tempOuterAvgNormalized.FR = this._tempNormalizerF.Interpolate(this._tempOuterOverLap.FR.Avg);
                this._tempOuterAvgNormalized.RL = this._tempNormalizerR.Interpolate(this._tempOuterOverLap.RL.Avg);
                this._tempOuterAvgNormalized.RR = this._tempNormalizerR.Interpolate(this._tempOuterOverLap.RR.Avg);
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
            } else if (RaceEngineerPlugin.Game.IsRf2) {
                var rawDataNew = (RfactorReader.RF2.WrapV2)data.NewData.GetRawDataObject();
                newTyreName = Encoding.ASCII.GetString(rawDataNew.CurrentPlayerTelemetry.mFrontTireCompoundName);
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
                this._presLoss[i] = 0.0;
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
                        this._presLoss[i] += presDelta[i];
                        this._presLossLap[i] = true;
                        RaceEngineerPlugin.LogInfo($"Pressure loss on {Names[i]} by {presDelta[i]}.");
                    }
                }
            } else {
                var areInputPressuresNotSet = double.IsNaN(this.CurrentInputPres[0]);
                // If tyre pressure changed suddenly while not moving, it's probably a tyre change and we need to take new input pressures
                static bool pred(double v) => Math.Abs(v) > 0.1;
                if (areInputPressuresNotSet || pred(presDelta[0]) || pred(presDelta[1]) || pred(presDelta[2]) || pred(presDelta[3])) {
                    this._currentInputPres[0] = Math.Ceiling(data.NewData.TyrePressureFrontLeft * 10.0) / 10.0;
                    this._currentInputPres[1] = Math.Ceiling(data.NewData.TyrePressureFrontRight * 10.0) / 10.0;
                    this._currentInputPres[2] = Math.Ceiling(data.NewData.TyrePressureRearLeft * 10.0) / 10.0;
                    this._currentInputPres[3] = Math.Ceiling(data.NewData.TyrePressureRearRight * 10.0) / 10.0;

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
                this._idealInputPres[iFront] = this.CurrentInputPres[iFront] + (this.Info.IdealPresRange.F.Avg - this._presOverLap[iFront].Avg);
                this._idealInputPres[iRear] = this.CurrentInputPres[iRear] + (this.Info.IdealPresRange.R.Avg - this._presOverLap[iRear].Avg);
            }
        }

        private void PredictIdealInputPressures(GameData data, Values v) {
            if (RaceEngineerPlugin.Game.IsAcc) {
                if (v.Weather.AirTemp == 0.0) {
                    // RaceEngineerPlugin.LogInfo($"Couldn't update ideal tyre pressures as 'tyreInfo == null' || 'AirTemp == 0.0'");
                    for (int i = 0; i < 4; i++) {
                        this._predictedIdealInputPresDry[i] = double.NaN;
                        this._predictedIdealInputPresNowWet[i] = double.NaN;
                        this._predictedIdealInputPresFutureWet[i] = double.NaN;
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
                        preds.CopyTo(this._predictedIdealInputPresDry);
                    } else {
                        if (v.Car.Setup != null && v.Track.Name != null && v.Car.Name != null) {
                            this.InitInputTyrePresPredictorDry(v.Track.Name, v.Car.Name, v.Car.Setup.advancedSetup.aeroBalance.brakeDuct.ToArray(), v.Db);
                            for (int i = 0; i < 4; i++) {
                                this._predictedIdealInputPresDry[i] = double.NaN;
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
                            this._predictedIdealInputPresNowWet[i] = double.NaN;
                        }
                    } else {
                        var preds = this.InputTyrePresPredictorNowWet.Predict(
                            v.Weather.AirTemp,
                            v.Weather.TrackTemp,
                            this.Info.IdealPresRange.F.Avg,
                            this.Info.IdealPresRange.R.Avg
                        );
                        preds.CopyTo(this._predictedIdealInputPresNowWet);
                    }
                }

                if (!this._updatingPresPredictorFutureWet && (rawDataNew.Graphics.rainIntensityIn30min != ACC_RAIN_INTENSITY.ACC_NO_RAIN || rawDataNew.Graphics.rainIntensityIn10min != ACC_RAIN_INTENSITY.ACC_NO_RAIN)) {
                    if (rawDataNew.Graphics.rainIntensityIn30min != rawDataOld.Graphics.rainIntensityIn30min || this.InputTyrePresPredictorFutureWet == null) {
                        if (v.Car.Setup != null && v.Track.Name != null && v.Car.Name != null) {
                            this.InitInputTyrePresPredictorFutureWet(v.Track.Name, v.Car.Name, v.Car.Setup.advancedSetup.aeroBalance.brakeDuct.ToArray(), data, v.Db);
                        }
                        for (int i = 0; i < 4; i++) {
                            this._predictedIdealInputPresFutureWet[i] = double.NaN;
                        }
                    } else {
                        var preds = this.InputTyrePresPredictorFutureWet.Predict(
                            v.Weather.AirTemp,
                            v.Weather.TrackTemp,
                            this.Info.IdealPresRange.F.Avg,
                            this.Info.IdealPresRange.R.Avg
                        );
                        preds.CopyTo(this._predictedIdealInputPresFutureWet);
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
                this._idealInputPres[i] = double.NaN;
                this._pressDeltaToIdeal[i] = double.NaN;
            }
            this._presOverLap.Reset();
            this._tempOverLap.Reset();
            this._tempInnerOverLap.Reset();
            this._tempMiddleOverLap.Reset();
            this._tempOuterOverLap.Reset();
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


    internal class InputTyrePresPredictor {
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