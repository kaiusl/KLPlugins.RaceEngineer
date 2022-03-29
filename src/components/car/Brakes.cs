using GameReaderCommon;
using SimHub.Plugins;
using System;
using KLPlugins.RaceEngineer.Stats;
using KLPlugins.RaceEngineer.Color;
using KLPlugins.RaceEngineer.RawData;

namespace KLPlugins.RaceEngineer.Car {
    public class Brakes {
        public int LapsNr { get; private set; }
        public int SetNr { get; private set; }
        public WheelsStats TempOverLap { get; }
        public ColorCalculator tempColor { get; private set; }
        public string[] TempColor { get; private set; }
        public string[] TempColorMin { get; private set; }
        public string[] TempColorMax { get; private set; }
        public string[] TempColorAvg { get; private set; }

        private WheelsRunningStats _tempRunning = new WheelsRunningStats();
        private DateTime _lastSampleTimeSec = DateTime.Now;

        public Brakes() {
            SetNr = 0;
            LapsNr = 0;
            TempOverLap = new WheelsStats();
            tempColor = new ColorCalculator(RaceEngineerPlugin.Settings.TempColor, RaceEngineerPlugin.Settings.BrakeTempColorDefValues);
            TempColor = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
            TempColor = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
            TempColorMin = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
            TempColorMax = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
            TempColorAvg = new string[4] { RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor, RaceEngineerPlugin.DefColor };
        }

        public void Reset() {
            SetNr = 0;
            LapsNr = 0;
            TempOverLap.Reset();
            for (int i = 0; i < 4; i++) {
                TempColor[i] = RaceEngineerPlugin.DefColor;
                TempColorMin[i] = RaceEngineerPlugin.DefColor;
                TempColorMax[i] = RaceEngineerPlugin.DefColor;
                TempColorAvg[i] = RaceEngineerPlugin.DefColor;
            }
            tempColor.UpdateInterpolation(RaceEngineerPlugin.Settings.BrakeTempColorDefValues);
            _tempRunning.Reset();
        }

        #region On... METHODS

        public void OnLapFinished(Values v) {
            LapsNr += 1;
            TempOverLap.Update(_tempRunning);
            _tempRunning.Reset();
            UpdateOverLapColors(v);
        }

        public void OnRegularUpdate(GameData data, Values v) {
            CheckPadChange(v);
            UpdateOverLapData(data, v);
            UpdateColors(data, v);
            
        }

        #endregion

        #region PRIVATE METHODS

        private void CheckPadChange(Values v) {
            // Other games don't have pad life properties
            if (!RaceEngineerPlugin.Game.IsAcc) return;

            // Pads can change at two moments:
            //    a) If we exit garage it's always new brakes
            //    b) If we change brakes in pit stop. Sudden change on ExitPitBox.

            if (v.Booleans.NewData.ExitedMenu || (v.Booleans.NewData.ExitedPitBox && v.RawData.NewData.Physics.padLife[0] > v.RawData.OldData.Physics.padLife[0])) {
                RaceEngineerPlugin.LogInfo("Brake pads changed.");
                SetNr += 1;
                LapsNr = 0;
            }
        }

        private void UpdateOverLapData(GameData data, Values v) {
            var now = data.NewData.PacketTime;
            var elapsedSec = (now - _lastSampleTimeSec).TotalSeconds;
            // Add sample to counters
            if (v.Booleans.NewData.IsMoving && v.Booleans.NewData.IsOnTrack && elapsedSec > 1) {
                double[] currentTemp = new double[] {
                    data.NewData.BrakeTemperatureFrontLeft,
                    data.NewData.BrakeTemperatureFrontRight,
                    data.NewData.BrakeTemperatureRearLeft,
                    data.NewData.BrakeTemperatureRearRight
                };
                _tempRunning.Update(currentTemp);

                _lastSampleTimeSec = now;
            }
        }

        private void UpdateColors(GameData data, Values v) {
            if (!v.Booleans.NewData.IsInMenu && (WheelFlags.Color & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                TempColor[0] = tempColor.GetColor(data.NewData.BrakeTemperatureFrontLeft).ToHEX();
                TempColor[1] = tempColor.GetColor(data.NewData.BrakeTemperatureFrontRight).ToHEX();
                TempColor[2] = tempColor.GetColor(data.NewData.BrakeTemperatureRearLeft).ToHEX();
                TempColor[3] = tempColor.GetColor(data.NewData.BrakeTemperatureRearRight).ToHEX();
            }
        }

        private void UpdateOverLapColors(Values v) {
            if ((WheelFlags.MinColor & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                TempColorMin[0] = tempColor.GetColor(TempOverLap[0].Min).ToHEX();
                TempColorMin[1] = tempColor.GetColor(TempOverLap[1].Min).ToHEX();
                TempColorMin[2] = tempColor.GetColor(TempOverLap[2].Min).ToHEX();
                TempColorMin[3] = tempColor.GetColor(TempOverLap[3].Min).ToHEX();
            }
            if ((WheelFlags.MaxColor & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                TempColorMax[0] = tempColor.GetColor(TempOverLap[0].Max).ToHEX();
                TempColorMax[1] = tempColor.GetColor(TempOverLap[1].Max).ToHEX();
                TempColorMax[2] = tempColor.GetColor(TempOverLap[2].Max).ToHEX();
                TempColorMax[3] = tempColor.GetColor(TempOverLap[3].Max).ToHEX();
            }
            if ((WheelFlags.AvgColor & RaceEngineerPlugin.Settings.BrakeTempFlags) != 0) {
                TempColorAvg[0] = tempColor.GetColor(TempOverLap[0].Avg).ToHEX();
                TempColorAvg[1] = tempColor.GetColor(TempOverLap[1].Avg).ToHEX();
                TempColorAvg[2] = tempColor.GetColor(TempOverLap[2].Avg).ToHEX();
                TempColorAvg[3] = tempColor.GetColor(TempOverLap[3].Avg).ToHEX();
            }

        }

        #endregion


    }


}