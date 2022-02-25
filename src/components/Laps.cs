using GameReaderCommon;
using SimHub.Plugins;
using System;
using RaceEngineerPlugin.Deque;

namespace RaceEngineerPlugin.Laps {

    public class Laps {
        private const string TAG = RaceEngineerPlugin.PLUGIN_NAME + " (Laps): ";

        public double LastTime { get; private set; }
        public FixedSizeDequeStats PrevTimes { get; private set; }
        public int StintNr { get; private set; }
        public int StintLaps { get; private set; }

        private double maxTime = 1000;

        public Laps() {
            StintNr = 0;
            StintLaps = 0;
            PrevTimes = new FixedSizeDequeStats(RaceEngineerPlugin.SETTINGS.NumPreviousValuesStored);
        }

        public void Reset() {
            LogInfo("Laps.Reset()");
            PrevTimes.Clear();
            maxTime = 1000;
            LastTime = 0.0;
            StintNr = 0;
            StintLaps = 0;
        }

        public void OnNewEvent(PluginManager pm, string carName, string trackName, Database.Database db) {
            OnNewSession(pm, carName, trackName, db);
        }


        public void OnNewSession(PluginManager pm, string carName, string trackName, Database.Database db) {
            Reset();
            int trackGrip = RaceEngineerPlugin.GAME.IsACC ? (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.trackGripStatus") : -1;

            foreach (Database.PrevData pd in db.GetPrevSessionData(carName, trackName, RaceEngineerPlugin.SETTINGS.NumPreviousValuesStored, trackGrip)) {
                LogInfo($"Read laptime '{pd.lapTime}' from database.");
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
            if (booleans.OldData.SaveLap && booleans.OldData.IsValidFuelLap && 0 < LastTime && LastTime < maxTime) {
                LogInfo($"Added laptime '{LastTime}' to deque.");
                PrevTimes.AddToFront(LastTime);
                maxTime = PrevTimes.Min + 30;
            }
        }

        private void LogInfo(string msq) {
            if (RaceEngineerPlugin.SETTINGS.Log) {
                SimHub.Logging.Current.Info(TAG + msq);
            }
        }



    }

}