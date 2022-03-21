using GameReaderCommon;
using SimHub.Plugins;
using System;
using RaceEngineerPlugin.Stats;
using RaceEngineerPlugin.Color;
using RaceEngineerPlugin.RawData;

namespace RaceEngineerPlugin.Car {
    public class Brakes {
        public int LapsNr { get; private set; }
        public int SetNr { get; private set; }
        public WheelsStats TempOverLap { get; }
        public ColorCalculator tempColor { get; private set; }
        public string[] TempColor { get; private set; }

        private WheelsRunningStats _tempRunning = new WheelsRunningStats();
        private DateTime _lastSampleTimeSec = DateTime.Now;

        public Brakes() {
            SetNr = 0;
            LapsNr = 0;
            TempOverLap = new WheelsStats();
            tempColor = new ColorCalculator(RaceEngineerPlugin.Settings.TempColor, RaceEngineerPlugin.Settings.BrakeTempColorDefValues);
            TempColor = new string[4] { "#000000", "#000000", "#000000", "#000000" };
        }

        public void Reset() {
            SetNr = 0;
            LapsNr = 0;
            TempOverLap.Reset();
            for (int i = 0; i < 4; i++) {
                TempColor[i] = "#000000";
            }
            tempColor.UpdateInterpolation(RaceEngineerPlugin.Settings.BrakeTempColorDefValues);
            _tempRunning.Reset();
        }

        #region On... METHODS

        public void OnLapFinished() {
            LapsNr += 1;
            TempOverLap.Update(_tempRunning);
            _tempRunning.Reset();
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

        #endregion


    }


}