using System;

using GameReaderCommon;

using KLPlugins.RaceEngineer.Color;
using KLPlugins.RaceEngineer.Stats;

namespace KLPlugins.RaceEngineer.Car {
    public class Brakes {
        public int LapsNr { get; private set; }
        public int SetNr { get; private set; }
        public WheelsStats TempOverLap { get; }
        public ColorCalculator TempColorCalculator { get; private set; }
        public string[] TempColor { get; private set; }
        public string[] TempColorMin { get; private set; }
        public string[] TempColorMax { get; private set; }
        public string[] TempColorAvg { get; private set; }

        private readonly WheelsRunningStats _tempRunning = new();
        private DateTime _lastSampleTimeSec = DateTime.Now;

        public Brakes() {
            this.SetNr = 0;
            this.LapsNr = 0;
            this.TempOverLap = new();
            this.TempColorCalculator = new(RaceEngineerPlugin.Settings.TempColor, RaceEngineerPlugin.Settings.BrakeTempColorDefValues);
            this.TempColor = [RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor];
            this.TempColor = [RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor];
            this.TempColorMin = [RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor];
            this.TempColorMax = [RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor];
            this.TempColorAvg = [RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor];
        }

        public void Reset() {
            this.SetNr = 0;
            this.LapsNr = 0;
            this.TempOverLap.Reset();
            for (int i = 0; i < 4; i++) {
                this.TempColor[i] = RaceEngineerPlugin.DefColor;
                this.TempColorMin[i] = RaceEngineerPlugin.DefColor;
                this.TempColorMax[i] = RaceEngineerPlugin.DefColor;
                this.TempColorAvg[i] = RaceEngineerPlugin.DefColor;
            }
            this.TempColorCalculator.UpdateInterpolation(RaceEngineerPlugin.Settings.BrakeTempColorDefValues);
            this._tempRunning.Reset();
        }

        #region On... METHODS

        public void OnLapFinished(Values v) {
            this.LapsNr += 1;
            this.TempOverLap.Update(this._tempRunning);
            this._tempRunning.Reset();
            this.UpdateOverLapColors(v);
        }

        public void OnRegularUpdate(GameData data, Values v) {
            this.CheckPadChange(data, v);
            this.UpdateOverLapData(data, v);
            this.UpdateColors(data, v);

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

        private void UpdateColors(GameData data, Values v) {
            if (!v.Booleans.NewData.IsInMenu && (WheelFlags.Color & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                this.TempColor[0] = this.TempColorCalculator.GetColor(data.NewData.BrakeTemperatureFrontLeft).ToHEX();
                this.TempColor[1] = this.TempColorCalculator.GetColor(data.NewData.BrakeTemperatureFrontRight).ToHEX();
                this.TempColor[2] = this.TempColorCalculator.GetColor(data.NewData.BrakeTemperatureRearLeft).ToHEX();
                this.TempColor[3] = this.TempColorCalculator.GetColor(data.NewData.BrakeTemperatureRearRight).ToHEX();
            }
        }

        private void UpdateOverLapColors(Values v) {
            if ((WheelFlags.MinColor & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                this.TempColorMin[0] = this.TempColorCalculator.GetColor(this.TempOverLap[0].Min).ToHEX();
                this.TempColorMin[1] = this.TempColorCalculator.GetColor(this.TempOverLap[1].Min).ToHEX();
                this.TempColorMin[2] = this.TempColorCalculator.GetColor(this.TempOverLap[2].Min).ToHEX();
                this.TempColorMin[3] = this.TempColorCalculator.GetColor(this.TempOverLap[3].Min).ToHEX();
            }
            if ((WheelFlags.MaxColor & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                this.TempColorMax[0] = this.TempColorCalculator.GetColor(this.TempOverLap[0].Max).ToHEX();
                this.TempColorMax[1] = this.TempColorCalculator.GetColor(this.TempOverLap[1].Max).ToHEX();
                this.TempColorMax[2] = this.TempColorCalculator.GetColor(this.TempOverLap[2].Max).ToHEX();
                this.TempColorMax[3] = this.TempColorCalculator.GetColor(this.TempOverLap[3].Max).ToHEX();
            }
            if ((WheelFlags.AvgColor & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                this.TempColorAvg[0] = this.TempColorCalculator.GetColor(this.TempOverLap[0].Avg).ToHEX();
                this.TempColorAvg[1] = this.TempColorCalculator.GetColor(this.TempOverLap[1].Avg).ToHEX();
                this.TempColorAvg[2] = this.TempColorCalculator.GetColor(this.TempOverLap[2].Avg).ToHEX();
                this.TempColorAvg[3] = this.TempColorCalculator.GetColor(this.TempOverLap[3].Avg).ToHEX();
            }

        }

        #endregion


    }


}