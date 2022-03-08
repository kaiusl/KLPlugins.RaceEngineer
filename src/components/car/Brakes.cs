using GameReaderCommon;
using SimHub.Plugins;
using System;
using RaceEngineerPlugin.Stats;
using RaceEngineerPlugin.Color;

namespace RaceEngineerPlugin.Car {
    public class Brakes {
        public int PadLaps { get; private set; }
        public int PadNr { get; private set; }
        public WheelsStats TempOverLap { get; }
        public ColorCalculator TempColor { get; private set; }

        private float[] prevPadLife = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f};
        private WheelsRunningStats tempRunning = new WheelsRunningStats();
        private double lastSampleTimeSec = DateTime.Now.Second;

        public Brakes() {
            PadLaps = 0;
            PadNr = 0;
            TempOverLap = new WheelsStats();
            TempColor = new ColorCalculator(RaceEngineerPlugin.SETTINGS.TempColor, RaceEngineerPlugin.SETTINGS.BrakeTempColorDefValues);
        }

        #region On... METHODS

        public void OnNewEvent() {
            PadNr = 0;
            PadLaps = 0;
        }

        public void OnLapFinished() {
            PadLaps += 1;
        }

        public void OnRegularUpdate(PluginManager pm, GameData data, Booleans.Booleans booleans) {
            CheckPadChange(pm, data);
            UpdateOverLapData(data, booleans);
        }

        #endregion

        #region PRIVATE METHODS

        private void CheckPadChange(PluginManager pm, GameData data) {
            if (RaceEngineerPlugin.GAME.IsACC) {
                var havePressuresChanged = data.NewData.SpeedKmh < 10 && (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife01") != 0 && (
                    Math.Abs(prevPadLife[0] - (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife01")) > 0.1
                    || Math.Abs(prevPadLife[1] - (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife02")) > 0.1
                    || Math.Abs(prevPadLife[2] - (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife03")) > 0.1
                    || Math.Abs(prevPadLife[3] - (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife04")) > 0.1);

                if (havePressuresChanged) {
                    RaceEngineerPlugin.LogInfo("Brake pads changed.");
                    PadNr += 1;
                    PadLaps = 0;
                }

                prevPadLife[0] = (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife01");
                prevPadLife[1] = (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife02");
                prevPadLife[2] = (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife03");
                prevPadLife[3] = (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife04");
            }
        }

        private void UpdateOverLapData(GameData data, Booleans.Booleans booleans) {
            double now = DateTime.Now.Second;
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