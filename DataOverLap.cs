using GameReaderCommon;
using System;
using System.Collections.Generic;

namespace RaceEngineerPlugin {
    /// <summary>
    /// Class to calculate statistics over a lap. Eg average tyre temperatures.
    /// </summary>
    public class DataOverLap {
        private WheelsRunningStats brakeTempRunning = new WheelsRunningStats();
        public WheelsStats BrakeTemp { get; }
        public double lastSampleTimeSeconds = DateTime.Now.Second;

        public DataOverLap() { 
            BrakeTemp = new WheelsStats();
        }

        public void Reset() {
            ResetRunningStats();
            BrakeTemp.Reset();
        }

        public void AddSample(GameData data, double nowSeconds) {
            double[] currentBrakeTemp = new double[] { 
                data.NewData.BrakeTemperatureFrontLeft, 
                data.NewData.BrakeTemperatureFrontRight, 
                data.NewData.BrakeTemperatureRearLeft, 
                data.NewData.BrakeTemperatureRearRight 
            };

            brakeTempRunning.Update(currentBrakeTemp);
            lastSampleTimeSeconds = nowSeconds;
        }

        public void StoreRunningStats() {
            BrakeTemp.Update(brakeTempRunning);
        }

        public void ResetRunningStats() {
            brakeTempRunning.Reset();
        }
    }
}