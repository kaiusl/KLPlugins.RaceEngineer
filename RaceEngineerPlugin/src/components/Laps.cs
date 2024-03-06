using System;

using GameReaderCommon;

using KLPlugins.RaceEngineer.Deque;

using SimHub.Plugins;

namespace KLPlugins.RaceEngineer.Laps {

    public class Laps {
        public double LastTime { get; private set; }
        public ReadonlyFixedSizeDequeStatsView PrevTimes => this._prevTimes.AsReadonlyView();
        public ReadonlyFixedSizeDequeStatsView PrevS1Times => this._prevS1Times.AsReadonlyView();
        public ReadonlyFixedSizeDequeStatsView PrevS2Times => this._prevS2Times.AsReadonlyView();
        public ReadonlyFixedSizeDequeStatsView PrevS3Times => this._prevS3Times.AsReadonlyView();
        public int StintNr { get; private set; }
        public int StintLaps { get; private set; }

        private readonly FixedSizeDequeStats _prevTimes;
        private readonly FixedSizeDequeStats _prevS1Times;
        private readonly FixedSizeDequeStats _prevS2Times;
        private readonly FixedSizeDequeStats _prevS3Times;

        private double _maxTime = 1000;

        internal Laps() {
            this.StintNr = 0;
            this.StintLaps = 0;
            var numValues = RaceEngineerPlugin.Settings.NumPreviousValuesStored;
            var outliersStrategy = RemoveOutliers.Upper;
            this._prevTimes = new(numValues, outliersStrategy);
            this._prevS1Times = new(numValues, outliersStrategy);
            this._prevS2Times = new(numValues, outliersStrategy);
            this._prevS3Times = new(numValues, outliersStrategy);
            this._prevTimes.Fill(double.NaN);
            this._prevS1Times.Fill(double.NaN);
            this._prevS2Times.Fill(double.NaN);
            this._prevS3Times.Fill(double.NaN);
        }

        internal void Reset() {
            RaceEngineerPlugin.LogInfo("Laps.Reset()");
            this._prevTimes.Fill(double.NaN);
            this._prevS1Times.Fill(double.NaN);
            this._prevS2Times.Fill(double.NaN);
            this._prevS3Times.Fill(double.NaN);
            this._maxTime = 1000;
            this.LastTime = 0.0;
            this.StintNr = 0;
            this.StintLaps = 0;
        }

        internal void OnNewEvent(GameData data, Values v) {
            foreach (Database.PrevData pd in v.Db.GetPrevSessionData(data, v)) {
                //RaceEngineerPlugin.LogInfo($"Read laptime '{pd.lapTime}' from database.");
                this._prevTimes.AddToFront(pd.lapTime);
            }
        }

        internal void OnNewSession(GameData data, Values v) {
            this.Reset();

            foreach (Database.PrevData pd in v.Db.GetPrevSessionData(data, v)) {
                //RaceEngineerPlugin.LogInfo($"Read laptime '{pd.lapTime}' from database.");
                this._prevTimes.AddToFront(pd.lapTime);
            }
        }

        internal void OnNewStint() {
            this.StintNr += 1;
            this.StintLaps = 0;
        }

        internal void OnLapFinished(GameData data, Values v) {
            this.StintLaps += 1;
            this.LastTime = data.NewData.LastLapTime.TotalSeconds;
            if (v.Booleans.NewData.SavePrevLap) {
                RaceEngineerPlugin.LogInfo($"Added laptime '{this.LastTime}' to deque.");
                this._prevTimes.AddToFront(this.LastTime);
                this._prevS1Times.AddToFront(data.NewData.Sector1LastLapTime?.TotalSeconds ?? -1.0);
                this._prevS2Times.AddToFront(data.NewData.Sector2LastLapTime?.TotalSeconds ?? -1.0);
                this._prevS3Times.AddToFront(data.NewData.Sector3LastLapTime?.TotalSeconds ?? -1.0);
                this._maxTime = this._prevTimes.Min + 30;
            }
        }
    }

}