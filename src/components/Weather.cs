using GameReaderCommon;
using SimHub.Plugins;
using System;
using RaceEngineerPlugin.Deque;

namespace RaceEngineerPlugin {

    public class Weather {
        public double AirTemp { get; private set; }
        public double TrackTemp { get; private set; }
        public double AirAtLapStart { get; private set; }
        public double TrackAtLapStart { get; private set; }

        public ACCEnums.RainIntensity RainIntensity { get; private set; }
        public ACCEnums.RainIntensity RainIntensityIn10Min { get; private set; }
        public ACCEnums.RainIntensity RainIntensityIn30Min { get; private set; }

        public bool RainIntensityChangedThisLap { get; private set; }

        public ACCEnums.RainIntensity PrevRainIntensity { get; private set; }
        public ACCEnums.RainIntensity PrevRainIntensityIn10Min { get; private set; }
        public ACCEnums.RainIntensity PrevRainIntensityIn30Min { get; private set; }

        // Keep track of weather changes, predict exact time for weather change

        public Weather() {
            Reset();
        }

        public void Reset() {
            AirAtLapStart = double.NaN;
            TrackAtLapStart = double.NaN;
            RainIntensity = ACCEnums.RainIntensity.NoRain;
            RainIntensityIn10Min = ACCEnums.RainIntensity.NoRain;
            RainIntensityIn30Min = ACCEnums.RainIntensity.NoRain;
            PrevRainIntensity = ACCEnums.RainIntensity.NoRain;
            PrevRainIntensityIn10Min = ACCEnums.RainIntensity.NoRain;
            PrevRainIntensityIn30Min = ACCEnums.RainIntensity.NoRain;
            RainIntensityChangedThisLap = false;
        }

        #region On... METHODS

        public void OnLapFinishedAfterInsert(GameData data) {
            AirAtLapStart = data.NewData.AirTemperature;
            TrackAtLapStart = data.NewData.RoadTemperature;
            RainIntensityChangedThisLap = false;
        }

        public void OnRegularUpdate(PluginManager pm, GameData data, Booleans.Booleans booleans) {
            if (booleans.NewData.EnteredMenu) {
                AirAtLapStart = double.NaN;
                TrackAtLapStart = double.NaN;
                RaceEngineerPlugin.LogInfo($"Reset temps at lap start to '{AirAtLapStart}' and '{TrackAtLapStart}'");
            }

            if (booleans.NewData.IsMoving && double.IsNaN(AirAtLapStart)) {
                bool set_lap_start_temps = false;

                // In race/hotstint take fuel start at the line, when the session timer starts running. Otherwise when we first start moving.
                if (data.NewData.SessionTypeName == "RACE" || data.NewData.SessionTypeName == "7") { // "7" is Simhubs value for HOTSTINT
                    if (data.OldData.SessionTimeLeft != data.NewData.SessionTimeLeft) {
                        set_lap_start_temps = true;
                    }
                } else {
                    set_lap_start_temps = true;
                }

                if (set_lap_start_temps) {
                    AirAtLapStart = data.NewData.AirTemperature;
                    TrackAtLapStart = data.NewData.RoadTemperature;
                    RaceEngineerPlugin.LogInfo($"Reset temps at lap start to '{AirAtLapStart}' and '{TrackAtLapStart}'");
                }
            }

            if (RaceEngineerPlugin.GAME.IsACC && data.NewData.AirTemperature == 0.0) {
                AirTemp = (float)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Realtime.AmbientTemp");
                TrackTemp = (float)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Realtime.TrackTemp");
            } else { 
                AirTemp = data.NewData.AirTemperature;
                TrackTemp = data.NewData.RoadTemperature;
            }

            if (RaceEngineerPlugin.GAME.IsACC) {
                PrevRainIntensity = RainIntensity;
                PrevRainIntensityIn10Min = RainIntensityIn10Min;
                PrevRainIntensityIn30Min = RainIntensityIn30Min;
                RainIntensity = (ACCEnums.RainIntensity)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.rainIntensity");
                RainIntensityIn10Min = (ACCEnums.RainIntensity)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.rainIntensityIn10Min");
                RainIntensityIn30Min = (ACCEnums.RainIntensity)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.rainIntensityIn30Min");

                if (!RainIntensityChangedThisLap && PrevRainIntensity != RainIntensity) {
                    RainIntensityChangedThisLap = true;
                }
            }

        }

        #endregion

    }

}