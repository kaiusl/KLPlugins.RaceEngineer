using GameReaderCommon;
using SimHub.Plugins;
using System;

namespace RaceEngineerPlugin {
    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    public class Values {
        private const string TAG = RaceEngineerPlugin.PLUGIN_NAME + " (Values): ";

        public Booleans.Booleans booleans = new Booleans.Booleans();
        public Car.Car car = new Car.Car();
        public Track.Track track = new Track.Track();
        public Laps.Laps laps = new Laps.Laps();

        public Remaining.RemainingInSession remainingInSession = new Remaining.RemainingInSession();
        public Remaining.RemainingOnFuel remainingOnFuel = new Remaining.RemainingOnFuel();

        public Database.Database db = new Database.Database();

        public Values() { }

        public void Dispose() {
            db.Dispose();
        }

        private void LogInfo(string msq) {
            if (RaceEngineerPlugin.SETTINGS.Log) {
                SimHub.Logging.Current.Info(TAG + msq);
            }
        }

        private void OnNewStint(PluginManager pm, GameData data) {
            LogInfo("OnNewStint.");
            laps.OnNewStint();
            car.OnNewStint(pm, db);
            db.InsertStint(pm, this, data);
        }

        public void OnNewEvent(PluginManager pm, GameData data) {
            LogInfo("OnNewEvent.");
            remainingInSession.Reset();
            remainingOnFuel.Reset();
            booleans.OnNewEvent(data.NewData.SessionTypeName);
            track.OnNewEvent(data);
            car.OnNewEvent(pm, data, db);
            laps.OnNewEvent(pm, car.Name, track.Name, db);
            db.InsertEvent(car.Name, track.Name);
        }

        /// <summary>
        /// Update all values. Run once per update cycle.
        /// </summary>
        public void OnRegularUpdate(PluginManager pm, GameData data) {
            booleans.OnRegularUpdate(pm, data, laps.PrevTimes.Min, car.Fuel.RemainingAtLapStart);
            track.OnRegularUpdate(data);
            car.OnRegularUpdate(pm, data, booleans, track.Name);

            // New stint starts at the pit exit. (ignore is session changes, for example from Qualy->Race this jump also happens but is undesired)
            if (data.OldData.IsInPitLane == 1 && data.NewData.IsInPitLane == 0
                && data.OldData.SessionTypeName == data.NewData.SessionTypeName) {
                LogInfo("New stint on pit exit.");
                OnNewStint(pm, data);
            }

            // We need to add stint at the start of the race/hotlap/hotstint separately since we are never in pitlane.
            if (data.NewData.SessionTypeName == "RACE" || data.NewData.SessionTypeName == "7" || data.NewData.SessionTypeName == "HOTLAP" && 
                !booleans.NewData.IsRaceStartStintAdded && booleans.NewData.IsMoving) 
            {
                LogInfo("New stint on race/hotlap/hotstint start.");
                OnNewStint(pm, data);
                booleans.RaceStartStintAdded();
            }

            remainingInSession.Update(booleans, data.NewData.SessionTimeLeft.TotalSeconds, data.NewData.RemainingLaps, car.Fuel.PrevUsedPerLap.Stats, laps.PrevTimes.Stats);
            remainingOnFuel.Update(car.Fuel.Remaining, car.Fuel.PrevUsedPerLap.Stats, laps.PrevTimes.Stats);

            if (booleans.NewData.HasFinishedLap) {
                LogInfo("Lap finished.");
                car.OnLapFinished(data, booleans);
                laps.OnLapFinished(data, booleans);
                db.InsertLap(pm, this, data);
            }

            if (data.OldData.SessionTypeName != data.NewData.SessionTypeName) {
                LogInfo("New session");
                booleans.OnNewSession(data.NewData.SessionTypeName);
                car.OnNewSession(pm, track.Name, db);
                laps.OnNewSession(pm, car.Name, track.Name, db);
            }

        }
    }


}