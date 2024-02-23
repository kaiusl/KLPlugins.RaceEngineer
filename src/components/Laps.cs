using System;

using GameReaderCommon;

using KLPlugins.RaceEngineer.Deque;

using SimHub.Plugins;

namespace KLPlugins.RaceEngineer.Laps {

    public class Laps {
        public double LastTime { get; private set; }
        public FixedSizeDequeStats PrevTimes { get; private set; }
        public FixedSizeDequeStats PrevS1Times { get; private set; }
        public FixedSizeDequeStats PrevS2Times { get; private set; }
        public FixedSizeDequeStats PrevS3Times { get; private set; }
        public int StintNr { get; private set; }
        public int StintLaps { get; private set; }

        private double _maxTime = 1000;

        public Laps() {
            this.StintNr = 0;
            this.StintLaps = 0;
            this.PrevTimes = new FixedSizeDequeStats(RaceEngineerPlugin.Settings.NumPreviousValuesStored, RemoveOutliers.Upper);
            this.PrevS1Times = new FixedSizeDequeStats(RaceEngineerPlugin.Settings.NumPreviousValuesStored, RemoveOutliers.Upper);
            this.PrevS2Times = new FixedSizeDequeStats(RaceEngineerPlugin.Settings.NumPreviousValuesStored, RemoveOutliers.Upper);
            this.PrevS3Times = new FixedSizeDequeStats(RaceEngineerPlugin.Settings.NumPreviousValuesStored, RemoveOutliers.Upper);
            this.PrevTimes.Fill(double.NaN);
            this.PrevS1Times.Fill(double.NaN);
            this.PrevS2Times.Fill(double.NaN);
            this.PrevS3Times.Fill(double.NaN);
        }

        public void Reset() {
            RaceEngineerPlugin.LogInfo("Laps.Reset()");
            this.PrevTimes.Fill(double.NaN);
            this.PrevS1Times.Fill(double.NaN);
            this.PrevS2Times.Fill(double.NaN);
            this.PrevS3Times.Fill(double.NaN);
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
                this.PrevS1Times.AddToFront(data.NewData.Sector1LastLapTime?.TotalSeconds ?? -1.0);
                this.PrevS2Times.AddToFront(data.NewData.Sector2LastLapTime?.TotalSeconds ?? -1.0);
                this.PrevS3Times.AddToFront(data.NewData.Sector3LastLapTime?.TotalSeconds ?? -1.0);
                this._maxTime = this.PrevTimes.Min + 30;
            }
        }
    }

}