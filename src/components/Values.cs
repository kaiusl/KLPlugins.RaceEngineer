using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Diagnostics;
using System.IO;
using ksBroadcastingNetwork;
using ksBroadcastingNetwork.Structs;
using System.Threading.Tasks;
using ACSharedMemory.ACC.Reader;

namespace RaceEngineerPlugin {

    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    public class Values : IDisposable {
        public Booleans.Booleans booleans = new Booleans.Booleans();
        public Car.Car car = new Car.Car();
        public Track.Track track = new Track.Track();
        public Laps.Laps laps = new Laps.Laps();
        public Weather weather = new Weather();
        public ACCRawData RawData;

        public Remaining.RemainingInSession remainingInSession = new Remaining.RemainingInSession();
        public Remaining.RemainingOnFuel remainingOnFuel = new Remaining.RemainingOnFuel();

        public Database.Database db = new Database.Database();

        private bool reset = true;

        public Values() {}

        public void Reset() {
            booleans.Reset(null);
            car.Reset();
            track.Reset();
            laps.Reset();
            weather.Reset();
            remainingInSession.Reset();
            remainingOnFuel.Reset();
            reset = true;
        }


        #region IDisposable Support
        ~Values() { 
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed = false; 
        protected virtual void Dispose(bool disposing) {
            if (!isDisposed) {
                if (disposing) {
                    RaceEngineerPlugin.LogInfo("Disposed");
                    db.Dispose();
                }

                isDisposed = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion

        private void OnNewStint(PluginManager pm, GameData data) {
            laps.OnNewStint();
            car.OnNewStint(pm, db);
            db.InsertStint(pm, this, data);
        }

        public void OnNewEvent(PluginManager pm, GameData data) {
            RaceEngineerPlugin.LogInfo("OnNewEvent.");
            remainingInSession.Reset();
            remainingOnFuel.Reset();
            booleans.OnNewEvent(data.NewData.SessionTypeName);
            track.OnNewEvent(data);
            int trackGrip = (int)RawData.Graphics.trackGripStatus;
            car.OnNewEvent(pm, data, trackGrip, db);
            laps.OnNewEvent(car.Name, track.Name, trackGrip, db);
            db.InsertEvent(car.Name, track.Name);
        }

        /// <summary>
        /// Update all values. Run once per update cycle.
        /// 
        /// We have multiple defined update points:
        ///     OnRegularUpdate - updated always
        ///     OnLapFinished - after the lap has finished, eg on the first point of new lap
        ///     OnNewStint - at the start of new stint, usually when we exit the pit lane
        ///     OnNewSession - at the start of new session, eg going from qualy to race
        ///     OnNewEvent - at the start of event, eg at the start of first session
        /// 
        /// </summary>
        public void OnDataUpdate(PluginManager pm, GameData data) {
            if (reset) { 
                reset = false;
            }

            RawData = (ACCRawData)data.NewData.GetRawDataObject();

            if (!booleans.NewData.IsGameRunning) {
                RaceEngineerPlugin.LogFileSeparator();
                OnNewEvent(pm, data);
            }

            booleans.OnRegularUpdate(pm, data, RawData, laps.PrevTimes.Min, car.Fuel.RemainingAtLapStart);
            track.OnRegularUpdate(data);
            car.OnRegularUpdate(pm, data, this);
            weather.OnRegularUpdate(pm, data, RawData, booleans);
            if (booleans.NewData.ExitedPitLane && data.OldData.SessionTypeName == data.NewData.SessionTypeName) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New stint on pit exit.");
                OnNewStint(pm, data);
            }

            // We need to add stint at the start of the race/hotlap/hotstint separately since we are never in pitlane.
            if ((data.NewData.SessionTypeName == "RACE" || data.NewData.SessionTypeName == "7" || data.NewData.SessionTypeName == "HOTLAP") && 
                !booleans.NewData.IsRaceStartStintAdded && booleans.NewData.IsMoving) 
            {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New stint on race/hotlap/hotstint start.");
                OnNewStint(pm, data);
                booleans.RaceStartStintAdded();
            }
            remainingInSession.OnRegularUpdate(booleans, data.NewData.SessionTimeLeft.TotalSeconds, data.NewData.RemainingLaps, car.Fuel.PrevUsedPerLap.Stats, laps.PrevTimes.Stats);
            remainingOnFuel.OnRegularUpdate(car.Fuel.Remaining, car.Fuel.PrevUsedPerLap.Stats, laps.PrevTimes.Stats);

            if (booleans.NewData.HasFinishedLap) {
                RaceEngineerPlugin.LogInfo("Lap finished.");
                booleans.OnLapFinished(data);
                car.OnLapFinished(pm, data, booleans);
                laps.OnLapFinished(data, booleans);
                if (laps.LastTime != 0) {
                    db.InsertLap(pm, this, data);
                }

                weather.OnLapFinishedAfterInsert(data);
                car.OnLapFinishedAfterInsert();
                RaceEngineerPlugin.LogFileSeparator();
            }

            if (data.OldData.SessionTypeName != data.NewData.SessionTypeName) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New session");
                booleans.OnNewSession(data.NewData.SessionTypeName);
                int trackGrip = (int)RawData.Graphics.trackGripStatus;
                car.OnNewSession(pm, track.Name, trackGrip, db);
                laps.OnNewSession(pm, car.Name, track.Name, trackGrip, db);
            }
        }

        public void OnGameNotRunning() {
            if (!reset) {
                Reset();
                booleans.OnGameNotRunning();
            }
        }

    }


}