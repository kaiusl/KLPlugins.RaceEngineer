using GameReaderCommon;
using SimHub.Plugins;
using System;
using RaceEngineerPlugin.Deque;
using System.Collections.Generic;
using ACSharedMemory.ACC.Reader;

namespace RaceEngineerPlugin {

    public class WeatherPoint { 
        public ACCEnums.RainIntensity RainIntensity { get; }
        public DateTime Time { get; }

        public WeatherPoint(ACCEnums.RainIntensity rainIntensity, DateTime time) {
            RainIntensity = rainIntensity;
            Time = time;
        }
    }


    public class Weather {
        public double AirTemp { get; private set; }
        public double TrackTemp { get; private set; }
        public double AirAtLapStart { get; private set; }
        public double TrackAtLapStart { get; private set; }

        public ACCEnums.RainIntensity? RainIntensity { get; private set; }
        public ACCEnums.RainIntensity? RainIntensityIn10Min { get; private set; }
        public ACCEnums.RainIntensity? RainIntensityIn30Min { get; private set; }

        public bool RainIntensityChangedThisLap { get; private set; }

        public ACCEnums.RainIntensity? PrevRainIntensity { get; private set; }
        public ACCEnums.RainIntensity? PrevRainIntensityIn10Min { get; private set; }
        public ACCEnums.RainIntensity? PrevRainIntensityIn30Min { get; private set; }

        public List<WeatherPoint> Future { get; }

        public string weatherSummary = "";

        // Keep track of weather changes, predict exact time for weather change

        public Weather() {
            Future = new List<WeatherPoint>();
            Reset();
        }

        public void Reset() {
            AirAtLapStart = double.NaN;
            TrackAtLapStart = double.NaN;
            RainIntensity = null;
            RainIntensityIn10Min = null;
            RainIntensityIn30Min = null;
            PrevRainIntensity = null;
            PrevRainIntensityIn10Min = null;
            PrevRainIntensityIn30Min = null;
            RainIntensityChangedThisLap = false;
            Future.Clear();
        }

        #region On... METHODS

        public void OnLapFinishedAfterInsert(GameData data) {
            AirAtLapStart = data.NewData.AirTemperature;
            TrackAtLapStart = data.NewData.RoadTemperature;
            RainIntensityChangedThisLap = false;
        }

        public void OnRegularUpdate(PluginManager pm, GameData data, ACCRawData rawData, Booleans.Booleans booleans) {
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
                AirTemp = rawData.Realtime.AmbientTemp;
                TrackTemp = rawData.Realtime.TrackTemp;
            } else { 
                AirTemp = data.NewData.AirTemperature;
                TrackTemp = data.NewData.RoadTemperature;
            }

            if (RaceEngineerPlugin.GAME.IsACC) {
                RainIntensity = (ACCEnums.RainIntensity)rawData.Graphics.rainIntensity;
                RainIntensityIn10Min = (ACCEnums.RainIntensity)rawData.Graphics.rainIntensityIn10min;
                RainIntensityIn30Min = (ACCEnums.RainIntensity)rawData.Graphics.rainIntensityIn30min;

                if (!RainIntensityChangedThisLap && PrevRainIntensity != RainIntensity) {
                    RainIntensityChangedThisLap = true;
                }

                if (PrevRainIntensityIn10Min != RainIntensityIn10Min) {
                    var now = data.NewData.PacketTime;
                    Future.Add(new WeatherPoint((ACCEnums.RainIntensity)RainIntensityIn10Min, now.AddMinutes(10)));
                    Future.Sort((a, b) => a.Time.CompareTo(b.Time));

                    weatherSummary = "";
                    foreach (var a in Future) {
                        weatherSummary += $"{a.Time.ToString("dd.MM.yyyy HH:mm.ss")} - {a.RainIntensity}; ";
                    }
                }
                if (PrevRainIntensityIn30Min != RainIntensityIn30Min) {
                    var now = data.NewData.PacketTime;
                    Future.Add(new WeatherPoint((ACCEnums.RainIntensity)RainIntensityIn10Min, now.AddMinutes(30)));
                    Future.Sort((a, b) => a.Time.CompareTo(b.Time));
                    weatherSummary = "";
                    foreach (var a in Future) {
                        weatherSummary += $"{a.Time.ToString("dd.MM.yyyy HH:mm.ss")} - {a.RainIntensity}; ";
                    }
                }

                PrevRainIntensity = RainIntensity;
                PrevRainIntensityIn10Min = RainIntensityIn10Min;
                PrevRainIntensityIn30Min = RainIntensityIn30Min;


            }

        }

        #endregion

    }

}