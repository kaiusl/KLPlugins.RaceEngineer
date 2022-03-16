using GameReaderCommon;
using SimHub.Plugins;
using System;
using RaceEngineerPlugin.Deque;

namespace RaceEngineerPlugin.Laps {

    public class Laps {
        public double LastTime { get; private set; }
        public FixedSizeDequeStats PrevTimes { get; private set; }
        public int StintNr { get; private set; }
        public int StintLaps { get; private set; }

        private double maxTime = 1000;

        public Laps() {
            StintNr = 0;
            StintLaps = 0;
            PrevTimes = new FixedSizeDequeStats(RaceEngineerPlugin.SETTINGS.NumPreviousValuesStored, RemoveOutliers.Upper);
        }

        public void Reset() {
            RaceEngineerPlugin.LogInfo("Laps.Reset()");
            PrevTimes.Clear();
            maxTime = 1000;
            LastTime = 0.0;
            StintNr = 0;
            StintLaps = 0;
        }

        public void OnNewEvent(string carName, string trackName, int trackGrip, Database.Database db) {
            foreach (Database.PrevData pd in db.GetPrevSessionData(carName, trackName, RaceEngineerPlugin.SETTINGS.NumPreviousValuesStored, trackGrip)) {
                RaceEngineerPlugin.LogInfo($"Read laptime '{pd.lapTime}' from database.");
                PrevTimes.AddToFront(pd.lapTime);
            }
        }


        public void OnNewSession(PluginManager pm, string carName, string trackName, int trackGrip, Database.Database db) {
            Reset();

            foreach (Database.PrevData pd in db.GetPrevSessionData(carName, trackName, RaceEngineerPlugin.SETTINGS.NumPreviousValuesStored, trackGrip)) {
                RaceEngineerPlugin.LogInfo($"Read laptime '{pd.lapTime}' from database.");
                PrevTimes.AddToFront(pd.lapTime);
            }
        }

        public void OnNewStint() {
            StintNr += 1;
            StintLaps = 0;
        }

        public void OnLapFinished(GameData data, Booleans.Booleans booleans) {
            StintLaps += 1;
            LastTime = data.NewData.LastLapTime.TotalSeconds;
            if (booleans.NewData.SavePrevLap && booleans.OldData.IsValidFuelLap && 0 < LastTime && LastTime < maxTime) {
                RaceEngineerPlugin.LogInfo($"Added laptime '{LastTime}' to deque.");
                PrevTimes.AddToFront(LastTime);
                maxTime = PrevTimes.Min + 30;
            }
        }
    }

}