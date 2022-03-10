using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Diagnostics;
using System.IO;
using ksBroadcastingNetwork;
using ksBroadcastingNetwork.Structs;

namespace RaceEngineerPlugin {

    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    public class Values : IDisposable {
        public Booleans.Booleans booleans = new Booleans.Booleans();
        public Car.Car car = new Car.Car();
        public Track.Track track = new Track.Track();
        public Laps.Laps laps = new Laps.Laps();
        public Temps temps = new Temps();
        public RealtimeUpdate realtimeUpdate;

        public ACCUdpRemoteClient broadcastClient;

        public Remaining.RemainingInSession remainingInSession = new Remaining.RemainingInSession();
        public Remaining.RemainingOnFuel remainingOnFuel = new Remaining.RemainingOnFuel();

        public Database.Database db = new Database.Database();

        public Values() {}

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
                    if (broadcastClient != null) {
                        broadcastClient.Shutdown();
                        broadcastClient.Dispose();
                        broadcastClient = null;
                    }
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
            if (broadcastClient != null) {
                broadcastClient.Shutdown();
                broadcastClient.Dispose();
                broadcastClient = null;
            }
            db.CommitTransaction();
            RaceEngineerPlugin.LogInfo("OnNewEvent.");
            remainingInSession.Reset();
            remainingOnFuel.Reset();
            booleans.OnNewEvent(data.NewData.SessionTypeName);
            track.OnNewEvent(data);
            car.OnNewEvent(pm, data, db);
            laps.OnNewEvent(pm, car.Name, track.Name, db);
            db.InsertEvent(car.Name, track.Name);
            ConnectToBroadcastClient();
        }

        public void ConnectToBroadcastClient() {
            broadcastClient = new ACCUdpRemoteClient("127.0.0.1", 9000, "REPlugin", "asd", "", 1000);
            broadcastClient.MessageHandler.OnRealtimeUpdate += OnBroadcastRealtimeUpdate;
            broadcastClient.MessageHandler.OnConnectionStateChanged += OnBroadcastConnectionStateChanged;
        }

        private void OnBroadcastRealtimeUpdate(string sender, RealtimeUpdate update) {
            realtimeUpdate = update;
        }

        private void OnBroadcastConnectionStateChanged(int connectionId, bool connectionSuccess, bool isReadonly, string error) {
            if (connectionSuccess) {
                RaceEngineerPlugin.LogInfo("Connected to broadcast client.");
            } else {
                RaceEngineerPlugin.LogWarn($"Failed to connect to broadcast client. Err: {error}");
            }
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
            if (!booleans.NewData.IsGameRunning) {
                RaceEngineerPlugin.LogFileSeparator();
                // We haven't updated any data, if we reached here it means tha game/event has started
                OnNewEvent(pm, data);
            }

            booleans.OnRegularUpdate(pm, data, laps.PrevTimes.Min, car.Fuel.RemainingAtLapStart);
            track.OnRegularUpdate(data);
            car.OnRegularUpdate(pm, data, this);
            temps.OnRegularUpdate(data, booleans);

            // New stint starts at the pit exit. (ignore is session changes, for example from Qualy->Race this jump also happens but is undesired)
            if (data.OldData.IsInPitLane == 1 && data.NewData.IsInPitLane == 0
                && data.OldData.SessionTypeName == data.NewData.SessionTypeName) {
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
                car.OnLapFinished(data, booleans);
                laps.OnLapFinished(data, booleans);
                if (laps.LastTime != 0) {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    db.InsertLap(pm, this, data);
                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    RaceEngineerPlugin.LogInfo($"Finished lap update in {ts.TotalMilliseconds}ms.");
                }

                temps.OnLapFinished(data);
                RaceEngineerPlugin.LogFileSeparator();
            }

            if (data.OldData.SessionTypeName != data.NewData.SessionTypeName) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New session");
                booleans.OnNewSession(data.NewData.SessionTypeName);
                car.OnNewSession(pm, track.Name, db);
                laps.OnNewSession(pm, car.Name, track.Name, db);
                db.CommitTransaction();
            }

            // Commit db if game is paused, or on track.
            if (booleans.OldData.IsOnTrack && !booleans.NewData.IsOnTrack) {
                db.CommitTransaction();
            }

        }

        public void OnGameNotRunning() {
            booleans.OnGameNotRunning();
            db.CommitTransaction();
            if (broadcastClient != null) {
                broadcastClient.Shutdown();
                broadcastClient.Dispose();
                broadcastClient = null;
            }
        }

    }


}