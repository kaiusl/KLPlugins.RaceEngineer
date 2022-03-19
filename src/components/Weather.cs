using GameReaderCommon;
using SimHub.Plugins;
using System;
using RaceEngineerPlugin.Deque;
using System.Collections.Generic;
using ACSharedMemory.ACC.MMFModels;
using RaceEngineerPlugin.RawData;

namespace RaceEngineerPlugin {

    public class WeatherPoint { 
        public ACC_RAIN_INTENSITY RainIntensity { get; }
        public DateTime Time { get; }

        public WeatherPoint(ACC_RAIN_INTENSITY rainIntensity, DateTime time) {
            RainIntensity = rainIntensity;
            Time = time;
        }
    }


    public class Weather {
        public double AirTemp { get; private set; }
        public double TrackTemp { get; private set; }
        public double AirTempAtLapStart { get; private set; }
        public double TrackTempAtLapStart { get; private set; }

        public List<WeatherPoint> Future { get; }

        public string weatherSummary = "";

        // Keep track of weather changes, predict exact time for weather change

        public Weather() {
            Future = new List<WeatherPoint>();
            Reset();
        }

        public void Reset() {
            AirTemp = double.NaN;
            TrackTemp = double.NaN;
            AirTempAtLapStart = double.NaN;
            TrackTempAtLapStart = double.NaN;
            Future.Clear();
        }

        #region On... METHODS

        public void OnLapFinishedAfterInsert(GameData data) {
            AirTempAtLapStart = data.NewData.AirTemperature;
            TrackTempAtLapStart = data.NewData.RoadTemperature;
        }

        public void OnRegularUpdate(GameData data, ACCRawData rawData, Booleans.Booleans booleans) {
            if (booleans.NewData.EnteredMenu) {
                AirTempAtLapStart = double.NaN;
                TrackTempAtLapStart = double.NaN;
                RaceEngineerPlugin.LogInfo($"Reset temps at lap start to '{AirTempAtLapStart}' and '{TrackTempAtLapStart}'");
            }

            if (booleans.NewData.IsMoving && double.IsNaN(AirTempAtLapStart)) {
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
                    AirTempAtLapStart = data.NewData.AirTemperature;
                    TrackTempAtLapStart = data.NewData.RoadTemperature;
                    RaceEngineerPlugin.LogInfo($"Reset temps at lap start to '{AirTempAtLapStart}' and '{TrackTempAtLapStart}'");
                }
            }

            if (RaceEngineerPlugin.GAME.IsACC && data.NewData.AirTemperature == 0.0) {
                AirTemp = rawData.NewData.Realtime.AmbientTemp;
                TrackTemp = rawData.NewData.Realtime.TrackTemp;
            } else {
                AirTemp = data.NewData.AirTemperature;
                TrackTemp = data.NewData.RoadTemperature;
            }


            if (RaceEngineerPlugin.GAME.IsACC) {
                
                if (rawData.OldData.Graphics.rainIntensityIn10min != rawData.NewData.Graphics.rainIntensityIn10min) {
                    var now = data.NewData.PacketTime;
                    Future.Add(new WeatherPoint(rawData.NewData.Graphics.rainIntensityIn10min, now.AddMinutes(10)));
                    Future.Sort((a, b) => a.Time.CompareTo(b.Time));

                    weatherSummary = "";
                    foreach (var a in Future) {
                        weatherSummary += $"{a.Time.ToString("dd.MM.yyyy HH:mm.ss")} - {a.RainIntensity}; ";
                    }
                }
                if (rawData.OldData.Graphics.rainIntensityIn30min != rawData.NewData.Graphics.rainIntensityIn30min) {
                    var now = data.NewData.PacketTime;
                    Future.Add(new WeatherPoint(rawData.NewData.Graphics.rainIntensityIn30min, now.AddMinutes(30)));
                    Future.Sort((a, b) => a.Time.CompareTo(b.Time));
                    weatherSummary = "";
                    foreach (var a in Future) {
                        weatherSummary += $"{a.Time.ToString("dd.MM.yyyy HH:mm.ss")} - {a.RainIntensity}; ";
                    }
                }
            }

        }

        #endregion

    }

}