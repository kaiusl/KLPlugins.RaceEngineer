using GameReaderCommon;
using SimHub.Plugins;
using System;

namespace RaceEngineerPlugin.Brakes {
    public class Brakes {
        private const string TAG = "RACE ENGINEER (Brakes): ";
        public int PadLaps { get; private set; }
        public int PadNr { get; private set; }
        public WheelsStats TempOverLap { get; }
        public Color.ColorCalculator TempColor { get; private set; }
        
        private float[] prevPadLife = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f};
        private WheelsRunningStats tempRunning = new WheelsRunningStats();
        private double lastSampleTimeSec = DateTime.Now.Second;

        public Brakes() {
            PadLaps = 0;
            PadNr = 0;
            TempOverLap = new WheelsStats();
            TempColor = new Color.ColorCalculator(RaceEngineerPlugin.SETTINGS.TempColor, RaceEngineerPlugin.SETTINGS.BrakeTempColorDefValues);
        }

        public void OnLapFinished() {
            PadLaps += 1;
        }

        public void CheckPadChange(PluginManager pm, GameData data) {
            if (RaceEngineerPlugin.GAME.IsACC) {
                var havePressuresChanged = data.NewData.SpeedKmh < 10 && (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife01") != 0 && (
                    Math.Abs(prevPadLife[0] - (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife01")) > 0.1
                    || Math.Abs(prevPadLife[1] - (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife02")) > 0.1
                    || Math.Abs(prevPadLife[2] - (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife03")) > 0.1
                    || Math.Abs(prevPadLife[3] - (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife04")) > 0.1);

                if (havePressuresChanged) {
                    LogInfo(String.Format("Brake pads changed."));
                    PadNr += 1;
                    PadLaps = 0;
                }

                prevPadLife[0] = (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife01");
                prevPadLife[1] = (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife02");
                prevPadLife[2] = (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife03");
                prevPadLife[3] = (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife04");
            }
        }

        public void UpdateOverLapData(GameData data, Booleans.Booleans booleans) {
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

        private void LogInfo(string msq) {
            SimHub.Logging.Current.Info(TAG + msq);
        }


    }


}