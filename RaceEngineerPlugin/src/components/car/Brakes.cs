using System;

using GameReaderCommon;

using KLPlugins.RaceEngineer.Interpolator;
using KLPlugins.RaceEngineer.Stats;

namespace KLPlugins.RaceEngineer.Car {
    public class Brakes {
        public int LapsNr { get; private set; }
        public int SetNr { get; private set; }
        public ReadonlyWheelsStatsView TempOverLap => this._tempOverLap.AsReadonlyView();
        public ReadonlyWheelsDataView<double> TempNormalized => this._tempNormalized.AsReadonlyView();
        public ReadonlyWheelsDataView<double> TempMinNormalized => this._tempMinNormalized.AsReadonlyView();
        public ReadonlyWheelsDataView<double> TempMaxNormalized => this._tempMaxNormalized.AsReadonlyView();
        public ReadonlyWheelsDataView<double> TempAvgNormalized => this._tempAvgNormalized.AsReadonlyView();

        // NOTE: It's important to never reassign these values. 
        // The property exports to SimHub rely on the fact that they point to one place always.
        private readonly WheelsStats _tempOverLap = new();
        private const double NORMALIZED_TEMP_DEF_VALUE = -1.0;
        private readonly WheelsData<double> _tempNormalized = new(NORMALIZED_TEMP_DEF_VALUE);
        private readonly WheelsData<double> _tempMinNormalized = new(NORMALIZED_TEMP_DEF_VALUE);
        private readonly WheelsData<double> _tempMaxNormalized = new(NORMALIZED_TEMP_DEF_VALUE);
        private readonly WheelsData<double> _tempAvgNormalized = new(NORMALIZED_TEMP_DEF_VALUE);
        private MultiPointLinearInterpolator _tempNormalizer;
        private readonly WheelsRunningStats _tempRunning = new();
        private DateTime _lastSampleTimeSec = DateTime.Now;

        internal Brakes() {
            this.SetNr = 0;
            this.LapsNr = 0;
            this._tempNormalizer = new MultiPointLinearInterpolator(RaceEngineerPlugin.Settings.BrakeTempNormalizationLut);
        }

        internal void Reset() {
            this.SetNr = 0;
            this.LapsNr = 0;
            this._tempOverLap.Reset();
            this._tempNormalizer = new MultiPointLinearInterpolator(RaceEngineerPlugin.Settings.BrakeTempNormalizationLut);
            this._tempNormalized.Reset();
            this._tempMinNormalized.Reset();
            this._tempMaxNormalized.Reset();
            this._tempAvgNormalized.Reset();
            this._tempRunning.Reset();
        }

        #region On... METHODS

        internal void OnLapFinished(Values v) {
            this.LapsNr += 1;
            this._tempOverLap.Update(this._tempRunning);
            this._tempRunning.Reset();
            this.UpdateNormalizedDataOverLap(v);
        }

        internal void OnRegularUpdate(GameData data, Values v) {
            this.CheckPadChange(data, v);
            this.UpdateOverLapData(data, v);
            this.UpdateNormalizedData(data, v);

        }

        #endregion

        #region PRIVATE METHODS

        private void CheckPadChange(GameData data, Values v) {
            // Other games don't have pad life properties
            if (!RaceEngineerPlugin.Game.IsAcc) return;

            var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();
            var rawDataOld = (ACSharedMemory.ACC.Reader.ACCRawData)data.OldData.GetRawDataObject();

            // Pads can change at two moments:
            //    a) If we exit garage it's always new brakes
            //    b) If we change brakes in pit stop. Sudden change on ExitPitBox.

            if (v.Booleans.NewData.ExitedMenu || (v.Booleans.NewData.ExitedPitBox && rawDataNew.Physics.padLife[0] > rawDataOld.Physics.padLife[0])) {
                RaceEngineerPlugin.LogInfo("Brake pads changed.");
                this.SetNr += 1;
                this.LapsNr = 0;
            }
        }

        private void UpdateOverLapData(GameData data, Values v) {
            var now = data.NewData.PacketTime;
            var elapsedSec = (now - this._lastSampleTimeSec).TotalSeconds;
            // Add sample to counters
            if (v.Booleans.NewData.IsMoving && v.Booleans.NewData.IsOnTrack && elapsedSec > 1) {
                double[] currentTemp = [
                    data.NewData.BrakeTemperatureFrontLeft,
                    data.NewData.BrakeTemperatureFrontRight,
                    data.NewData.BrakeTemperatureRearLeft,
                    data.NewData.BrakeTemperatureRearRight
                ];
                this._tempRunning.Update(currentTemp);

                this._lastSampleTimeSec = now;
            }
        }

        private void UpdateNormalizedData(GameData data, Values v) {
            if (!v.Booleans.NewData.IsInMenu && (WheelFlags.Color & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                this._tempNormalized.FL = this._tempNormalizer.Interpolate(data.NewData.BrakeTemperatureFrontLeft);
                this._tempNormalized.FR = this._tempNormalizer.Interpolate(data.NewData.BrakeTemperatureFrontRight);
                this._tempNormalized.RL = this._tempNormalizer.Interpolate(data.NewData.BrakeTemperatureRearLeft);
                this._tempNormalized.RR = this._tempNormalizer.Interpolate(data.NewData.BrakeTemperatureRearRight);
            }
        }

        private void UpdateNormalizedDataOverLap(Values v) {
            if ((WheelFlags.MinColor & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                for (int i = 0; i < 4; i++) {
                    this._tempMinNormalized[0] = this._tempNormalizer.Interpolate(this.TempOverLap[0].Min);
                }
            }
            if ((WheelFlags.MaxColor & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                for (int i = 0; i < 4; i++) {
                    this._tempMaxNormalized[0] = this._tempNormalizer.Interpolate(this.TempOverLap[0].Max);
                }
            }
            if ((WheelFlags.AvgColor & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                for (int i = 0; i < 4; i++) {
                    this._tempAvgNormalized[0] = this._tempNormalizer.Interpolate(this.TempOverLap[0].Avg);
                }
            }

        }

        #endregion


    }


}