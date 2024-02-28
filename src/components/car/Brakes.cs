using System;

using GameReaderCommon;

using KLPlugins.RaceEngineer.Color;
using KLPlugins.RaceEngineer.Stats;

namespace KLPlugins.RaceEngineer.Car {
    public class WheelsData<T> {
        private Func<T> _defGenerator { get; set; }
        private T[] _data { get; set; } = new T[4];

        public WheelsData(Func<T> defGenerator) {
            this._defGenerator = defGenerator;
            for (int i = 0; i < 4; i++) {
                this._data[i] = this._defGenerator();
            }
        }

        public WheelsData(T def) : this(() => def) { }

        public void Reset() {
            for (int i = 0; i < 4; i++) {
                this._data[i] = this._defGenerator();
            }
        }

        public T FL { get => this._data[0]; set => this._data[0] = value; }
        public T FR { get => this._data[1]; set => this._data[1] = value; }
        public T RL { get => this._data[2]; set => this._data[2] = value; }
        public T RR { get => this._data[3]; set => this._data[3] = value; }

        public T this[int index] {
            get => this._data[index];
            set => this._data[index] = value;
        }
    }

    public class Brakes {
        public int LapsNr { get; private set; }
        public int SetNr { get; private set; }
        public WheelsStats TempOverLap { get; }

        public MultiPointLinearInterpolator TempNormalizer { get; private set; }
        public WheelsData<double> TempNormalized { get; } = new(0.0);
        public WheelsData<double> TempMinNormalized { get; } = new(0.0);
        public WheelsData<double> TempMaxNormalized { get; } = new(0.0);
        public WheelsData<double> TempAvgNormalized { get; } = new(0.0);

        private readonly WheelsRunningStats _tempRunning = new();
        private DateTime _lastSampleTimeSec = DateTime.Now;

        public Brakes() {
            this.SetNr = 0;
            this.LapsNr = 0;
            this.TempOverLap = new();
            this.TempNormalizer = new MultiPointLinearInterpolator(RaceEngineerPlugin.Settings.TyreTempNormalizationLut);
        }

        public void Reset() {
            this.SetNr = 0;
            this.LapsNr = 0;
            this.TempOverLap.Reset();
            this.TempNormalizer = new MultiPointLinearInterpolator(RaceEngineerPlugin.Settings.TyreTempNormalizationLut);
            this.TempNormalized.Reset();
            this.TempMinNormalized.Reset();
            this.TempMaxNormalized.Reset();
            this.TempAvgNormalized.Reset();
            this._tempRunning.Reset();
        }

        #region On... METHODS

        public void OnLapFinished(Values v) {
            this.LapsNr += 1;
            this.TempOverLap.Update(this._tempRunning);
            this._tempRunning.Reset();
            this.UpdateNormalizedDataOverLap(v);
        }

        public void OnRegularUpdate(GameData data, Values v) {
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
                this.TempNormalized.FL = this.TempNormalizer.Interpolate(data.NewData.BrakeTemperatureFrontLeft);
                this.TempNormalized.FR = this.TempNormalizer.Interpolate(data.NewData.BrakeTemperatureFrontRight);
                this.TempNormalized.RL = this.TempNormalizer.Interpolate(data.NewData.BrakeTemperatureRearLeft);
                this.TempNormalized.RR = this.TempNormalizer.Interpolate(data.NewData.BrakeTemperatureRearRight);
            }
        }

        private void UpdateNormalizedDataOverLap(Values v) {
            if ((WheelFlags.MinColor & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                for (int i = 0; i < 4; i++) {
                    this.TempMinNormalized[0] = this.TempNormalizer.Interpolate(this.TempOverLap[0].Min);
                }
            }
            if ((WheelFlags.MaxColor & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                for (int i = 0; i < 4; i++) {
                    this.TempMaxNormalized[0] = this.TempNormalizer.Interpolate(this.TempOverLap[0].Max);
                }
            }
            if ((WheelFlags.AvgColor & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                for (int i = 0; i < 4; i++) {
                    this.TempAvgNormalized[0] = this.TempNormalizer.Interpolate(this.TempOverLap[0].Avg);
                }
            }

        }

        #endregion


    }


}