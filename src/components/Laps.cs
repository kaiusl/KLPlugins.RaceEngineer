using System;

using GameReaderCommon;

using KLPlugins.RaceEngineer.Deque;

using SimHub.Plugins;

namespace KLPlugins.RaceEngineer.Laps {

    public class Laps {
        public double LastTime { get; private set; }
        public FixedSizeDequeStats PrevTimes { get; private set; }
        public int StintNr { get; private set; }
        public int StintLaps { get; private set; }

        private double _maxTime = 1000;

        public Laps() {
            this.StintNr = 0;
            this.StintLaps = 0;
            this.PrevTimes = new FixedSizeDequeStats(RaceEngineerPlugin.Settings.NumPreviousValuesStored, RemoveOutliers.Upper);
            this.PrevTimes.Fill(double.NaN);
        }

        public void Reset() {
            RaceEngineerPlugin.LogInfo("Laps.Reset()");
            this.PrevTimes.Fill(double.NaN);
            this._maxTime = 1000;
            this.LastTime = 0.0;
            this.StintNr = 0;
            this.StintLaps = 0;
        }

        public void OnNewEvent(Values v) {
            foreach (Database.PrevData pd in v.Db.GetPrevSessionData(v)) {
                RaceEngineerPlugin.LogInfo($"Read laptime '{pd.lapTime}' from database.");
                this.PrevTimes.AddToFront(pd.lapTime);
            }
        }

        public void OnNewSession(Values v) {
            this.Reset();

            foreach (Database.PrevData pd in v.Db.GetPrevSessionData(v)) {
                RaceEngineerPlugin.LogInfo($"Read laptime '{pd.lapTime}' from database.");
                this.PrevTimes.AddToFront(pd.lapTime);
            }
        }

        public void OnNewStint() {
            this.StintNr += 1;
            this.StintLaps = 0;
        }

        public void OnLapFinished(GameData data, Values v) {
            this.StintLaps += 1;
            this.LastTime = data.NewData.LastLapTime.TotalSeconds;
            if (v.Booleans.NewData.SavePrevLap) {
                RaceEngineerPlugin.LogInfo($"Added laptime '{this.LastTime}' to deque.");
                this.PrevTimes.AddToFront(this.LastTime);
                this._maxTime = this.PrevTimes.Min + 30;
            }
        }
    }

}