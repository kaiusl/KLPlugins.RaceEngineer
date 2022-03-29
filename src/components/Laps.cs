using GameReaderCommon;
using SimHub.Plugins;
using System;
using KLPlugins.RaceEngineer.Deque;

namespace KLPlugins.RaceEngineer.Laps {

    public class Laps {
        public double LastTime { get; private set; }
        public FixedSizeDequeStats PrevTimes { get; private set; }
        public int StintNr { get; private set; }
        public int StintLaps { get; private set; }

        private double _maxTime = 1000;

        public Laps() {
            StintNr = 0;
            StintLaps = 0;
            PrevTimes = new FixedSizeDequeStats(RaceEngineerPlugin.Settings.NumPreviousValuesStored, RemoveOutliers.Upper);
            PrevTimes.Fill(double.NaN);
        }

        public void Reset() {
            RaceEngineerPlugin.LogInfo("Laps.Reset()");
            PrevTimes.Fill(double.NaN);
            _maxTime = 1000;
            LastTime = 0.0;
            StintNr = 0;
            StintLaps = 0;
        }

        public void OnNewEvent(Values v) {
            foreach (Database.PrevData pd in v.Db.GetPrevSessionData(v)) {
                RaceEngineerPlugin.LogInfo($"Read laptime '{pd.lapTime}' from database.");
                PrevTimes.AddToFront(pd.lapTime);
            }
        }

        public void OnNewSession(Values v) {
            Reset();

            foreach (Database.PrevData pd in v.Db.GetPrevSessionData(v)) {
                RaceEngineerPlugin.LogInfo($"Read laptime '{pd.lapTime}' from database.");
                PrevTimes.AddToFront(pd.lapTime);
            }
        }

        public void OnNewStint() {
            StintNr += 1;
            StintLaps = 0;
        }

        public void OnLapFinished(GameData data, Values v) {
            StintLaps += 1;
            LastTime = data.NewData.LastLapTime.TotalSeconds;
            if (v.Booleans.NewData.SavePrevLap) {
                RaceEngineerPlugin.LogInfo($"Added laptime '{LastTime}' to deque.");
                PrevTimes.AddToFront(LastTime);
                _maxTime = PrevTimes.Min + 30;
            }
        }
    }

}