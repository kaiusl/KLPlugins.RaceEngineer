using GameReaderCommon;
using SimHub.Plugins;
using System;
using RaceEngineerPlugin.Deque;

namespace RaceEngineerPlugin {

    public class Temps {
        public double AirAtLapStart { get; private set; }
        public double TrackAtLapStart { get; private set; }

        public Temps() {
            AirAtLapStart = double.NaN;
            TrackAtLapStart = double.NaN;
        }

        public void Reset() {
            AirAtLapStart = double.NaN;
            TrackAtLapStart = double.NaN;
        }

        #region On... METHODS

        public void OnLapFinished(GameData data) {
            AirAtLapStart = data.NewData.AirTemperature;
            TrackAtLapStart = data.NewData.RoadTemperature;
        }

        public void OnRegularUpdate(GameData data, Booleans.Booleans booleans) {
            // This happens when we jump to pits, reset fuel
            if (data.OldData.AirTemperature != 0.0 && data.NewData.AirTemperature == 0.0) {
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
        }

        #endregion

    }

}