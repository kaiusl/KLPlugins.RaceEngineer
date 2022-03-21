using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Diagnostics;
using System.IO;
using ksBroadcastingNetwork;
using ksBroadcastingNetwork.Structs;
using System.Threading.Tasks;
using RaceEngineerPlugin.RawData;
using SHACCRawData = ACSharedMemory.ACC.Reader.ACCRawData;

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
        public ACCRawData RawData = new ACCRawData();

        public Remaining.RemainingInSession remainingInSession = new Remaining.RemainingInSession();
        public Remaining.RemainingOnFuel remainingOnFuel = new Remaining.RemainingOnFuel();

        public Database.Database db = new Database.Database();

        private bool reset = true;

        public Values() {}

        public void Reset() {
            booleans.Reset();
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

        private void OnNewStint(GameData data) {
            laps.OnNewStint();
            car.OnNewStint();
            db.InsertStint(data, this);
        }

        public void OnNewEvent(GameData data) {
            RaceEngineerPlugin.LogInfo("OnNewEvent.");
            remainingInSession.Reset();
            remainingOnFuel.Reset();
            booleans.OnNewEvent(RawData.NewData.Realtime.SessionType);
            track.OnNewEvent(data);
            int trackGrip = (int)RawData.NewData.Graphics.trackGripStatus;
            car.OnNewEvent(data, this);
            laps.OnNewEvent(this);
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
        public void OnDataUpdate(GameData data) {
            if (reset) { 
                reset = false;
            }

            RawData.Update((SHACCRawData)data.NewData.GetRawDataObject());

            if (!booleans.NewData.IsGameRunning) {
                RaceEngineerPlugin.LogFileSeparator();
                OnNewEvent(data);
            }

            booleans.OnRegularUpdate(data, this);
            track.OnRegularUpdate(data);
            car.OnRegularUpdate(data, this);
            weather.OnRegularUpdate(data, RawData, booleans);


            var sessTypeNew = RawData.NewData.Realtime.SessionType;
            if (booleans.NewData.ExitedPitLane && sessTypeNew == RawData.OldData.Realtime.SessionType) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New stint on pit exit.");
                OnNewStint(data);
            }

            // We need to add stint at the start of the race/hotlap/hotstint separately since we are never in pitlane.
            if (!booleans.NewData.IsRaceStartStintAdded && booleans.NewData.IsMoving && (sessTypeNew == RaceSessionType.Race || sessTypeNew == RaceSessionType.Hotstint || sessTypeNew == RaceSessionType.Hotlap)) 
            {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New stint on race/hotlap/hotstint start.");
                OnNewStint(data);
                booleans.RaceStartStintAdded();
            }

            remainingInSession.OnRegularUpdate(this, data.NewData.SessionTimeLeft.TotalSeconds, data.NewData.RemainingLaps);
            remainingOnFuel.OnRegularUpdate(this);

            if (booleans.NewData.HasFinishedLap) {
                RaceEngineerPlugin.LogInfo("Lap finished.");
                booleans.OnLapFinished(data);
                car.OnLapFinished(data, this);
                laps.OnLapFinished(data, this);
                if (laps.LastTime != 0) {
                    db.InsertLap(data, this);
                }

                weather.OnLapFinishedAfterInsert(data);
                car.OnLapFinishedAfterInsert();
                RaceEngineerPlugin.LogFileSeparator();
            }

            if (sessTypeNew != RawData.OldData.Realtime.SessionType) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New session");
                booleans.OnNewSession(sessTypeNew);
                int trackGrip = (int)RawData.NewData.Graphics.trackGripStatus;
                car.OnNewSession(this);
                laps.OnNewSession(this);
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