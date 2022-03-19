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

        private float prevPadLife = 0.0f;
        private WheelsRunningStats tempRunning = new WheelsRunningStats();
        private double lastSampleTimeSec = DateTime.Now.Second;

        public Brakes() {
            LapsNr = 0;
            SetNr = 0;
            TempOverLap = new WheelsStats();
            tempColor = new ColorCalculator(RaceEngineerPlugin.SETTINGS.TempColor, RaceEngineerPlugin.SETTINGS.BrakeTempColorDefValues);
            TempColor = new string[4] { "#000000", "#000000", "#000000", "#000000" };
        }

        public void Reset() {
            SetNr = 0;
            LapsNr = 0;
            TempOverLap.Reset();
            for (int i = 0; i < 4; i++) {
                TempColor[i] = "#000000";
            }
            tempColor.UpdateInterpolation(RaceEngineerPlugin.SETTINGS.BrakeTempColorDefValues);

            prevPadLife = 0.0f;
            tempRunning.Reset();
        }

        #region On... METHODS

        public void OnLapFinished() {
            LapsNr += 1;
        }

        public void OnRegularUpdate(GameData data, ACCRawData rawData, Booleans.Booleans booleans) {
            CheckPadChange(rawData, booleans);
            UpdateOverLapData(data, booleans);

            if (!booleans.NewData.IsInMenu && (WheelFlags.Color & RaceEngineerPlugin.SETTINGS.BrakeTempFlags) != 0) {
                TempColor[0] = tempColor.GetColor(data.NewData.BrakeTemperatureFrontLeft).ToHEX();
                TempColor[1] = tempColor.GetColor(data.NewData.BrakeTemperatureFrontRight).ToHEX();
                TempColor[2] = tempColor.GetColor(data.NewData.BrakeTemperatureRearLeft).ToHEX();
                TempColor[3] = tempColor.GetColor(data.NewData.BrakeTemperatureRearRight).ToHEX();
            }
        }

        #endregion

        #region PRIVATE METHODS

        private void CheckPadChange(ACCRawData rawData, Booleans.Booleans booleans) {
            // Other games don't have pad life properties
            if (!RaceEngineerPlugin.GAME.IsACC) return;

            // Pads can change at two moments:
            //    a) If we exit garage it's always new brakes
            //    b) If we change brakes in pit stop. Sudden change on ExitPitBox.

            if (booleans.NewData.ExitedMenu || (booleans.NewData.ExitedPitBox && rawData.NewData.Physics.padLife[0] > rawData.OldData.Physics.padLife[0])) {
                RaceEngineerPlugin.LogInfo("Brake pads changed.");
                SetNr += 1;
                LapsNr = 0;
            }
        }

        private void UpdateOverLapData(GameData data, Booleans.Booleans booleans) {
            double now = data.FrameTime.Second;
            if (booleans.NewData.HasFinishedLap) {
                // Copy last lap results and reset counters
                TempOverLap.Update(tempRunning);
                tempRunning.Reset();
            }

            // Add sample to counters
            if (booleans.NewData.IsMoving && booleans.NewData.IsOnTrack && lastSampleTimeSec != now) {
                double[] currentTemp = new double[] {
                    data.NewData.BrakeTemperatureFrontLeft,
                    data.NewData.BrakeTemperatureFrontRight,
                    data.NewData.BrakeTemperatureRearLeft,
                    data.NewData.BrakeTemperatureRearRight
                };
                tempRunning.Update(currentTemp);

                lastSampleTimeSec = now;
            }
        }

        #endregion


    }


}