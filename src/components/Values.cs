using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Diagnostics;
using System.IO;
using ksBroadcastingNetwork;
using ksBroadcastingNetwork.Structs;
using System.Threading.Tasks;

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
        public RealtimeUpdate realtimeUpdate = null;

        public ACCUdpRemoteClient broadcastClient;

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
            temps.Reset();
            realtimeUpdate = null;
            if (broadcastClient != null) {
                DisposeBroadcastClient();
            }
            remainingInSession.Reset();
            remainingOnFuel.Reset();
            db.CommitTransactionLocking();
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
                    DisposeBroadcastClient();
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
                RaceEngineerPlugin.LogWarn("Broadcast client wasn't 'null' at start of new event. Shouldn't be possible, there is a bug in disposing of Broadcast client from previous session.");
                DisposeBroadcastClient();
            }
            db.CommitTransactionLocking();
            RaceEngineerPlugin.LogInfo("OnNewEvent.");
            remainingInSession.Reset();
            remainingOnFuel.Reset();
            booleans.OnNewEvent(data.NewData.SessionTypeName);
            track.OnNewEvent(data);
            int trackGrip = (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.trackGripStatus");
            car.OnNewEvent(pm, data, trackGrip, db);
            laps.OnNewEvent(pm, car.Name, track.Name, trackGrip, db);
            db.InsertEvent(car.Name, track.Name);
            ConnectToBroadcastClient();
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

            //Stopwatch stopwatch = new Stopwatch();
            //Stopwatch sw = new Stopwatch();
            //stopwatch.Start();
            //sw.Start();

            if (!booleans.NewData.IsGameRunning) {
                RaceEngineerPlugin.LogFileSeparator();
                // We haven't updated any data, if we reached here it means tha game/event has started
                OnNewEvent(pm, data);
            }

            //sw.Restart();
            booleans.OnRegularUpdate(pm, data, laps.PrevTimes.Min, car.Fuel.RemainingAtLapStart);
            //sw.Stop();
            //stopwatch.Stop();
            //var ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\RETiming_Booleans_OnRegularUpdate_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //stopwatch.Start();
            //sw.Restart();
            track.OnRegularUpdate(data);
            //sw.Stop();
            //stopwatch.Stop();
            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\RETiming_Track_OnRegularUpdate_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //stopwatch.Start();
            //sw.Restart();
            car.OnRegularUpdate(pm, data, this);
            //sw.Stop();
            //stopwatch.Stop();
            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\RETiming_Car_OnRegularUpdate_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //stopwatch.Start();
            //sw.Restart();
            temps.OnRegularUpdate(data, booleans);
            //sw.Stop();
            //stopwatch.Stop();
            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\RETiming_temps_OnRegularUpdate_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //stopwatch.Start();
            //sw.Restart();
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
            //sw.Stop();
            //stopwatch.Stop();
            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\RETiming_OnNewStint_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //stopwatch.Start();
            //sw.Restart();
            remainingInSession.OnRegularUpdate(booleans, data.NewData.SessionTimeLeft.TotalSeconds, data.NewData.RemainingLaps, car.Fuel.PrevUsedPerLap.Stats, laps.PrevTimes.Stats);
            remainingOnFuel.OnRegularUpdate(car.Fuel.Remaining, car.Fuel.PrevUsedPerLap.Stats, laps.PrevTimes.Stats);
            //sw.Stop();
            //stopwatch.Stop();
            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\RETiming_Remainings_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //stopwatch.Start();
            //sw.Restart();
            if (booleans.NewData.HasFinishedLap) {
                RaceEngineerPlugin.LogInfo("Lap finished.");
                booleans.OnLapFinished(data);
                car.OnLapFinished(pm, data, booleans);
                laps.OnLapFinished(data, booleans);
                if (laps.LastTime != 0) {
                    db.InsertLap(pm, this, data);
                }

                temps.OnLapFinishedAfterInsert(data);
                car.OnLapFinishedAfterInsert();
                RaceEngineerPlugin.LogFileSeparator();
            }
            //sw.Stop();
            //stopwatch.Stop();
            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\RETiming_OnLapFinished_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //stopwatch.Start();
            //sw.Restart();
            if (data.OldData.SessionTypeName != data.NewData.SessionTypeName) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New session");
                booleans.OnNewSession(data.NewData.SessionTypeName);
                int trackGrip = (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.trackGripStatus");
                car.OnNewSession(pm, track.Name, trackGrip, db);
                laps.OnNewSession(pm, car.Name, track.Name, trackGrip, db);
                db.CommitTransactionLocking();
            }
            //sw.Stop();
            //stopwatch.Stop();
            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\RETiming_OnNewSession_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //stopwatch.Start();
            //sw.Restart();
            // Commit db if game is paused, or on track.
            if (booleans.OldData.IsOnTrack && !booleans.NewData.IsOnTrack) {
                db.CommitTransactionLocking();
            }
            //sw.Stop();
            //stopwatch.Stop();

            //ts = sw.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\RETiming_DBCommit_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");
            //ts = stopwatch.Elapsed;
            //File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\RETiming_Total_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

        }

        public void OnGameNotRunning() {
            if (!reset) {
                Reset();
                booleans.OnGameNotRunning();
            }
        }

        #region Broadcast client connection
        public void ConnectToBroadcastClient() {
            broadcastClient = new ACCUdpRemoteClient("127.0.0.1", 9000, "REPlugin", "asd", "", 1000);
            broadcastClient.MessageHandler.OnRealtimeUpdate += OnBroadcastRealtimeUpdate;
            broadcastClient.MessageHandler.OnConnectionStateChanged += OnBroadcastConnectionStateChanged;
        }

        public void DisposeBroadcastClient() {
            if (broadcastClient != null) {
                broadcastClient.Shutdown();
                broadcastClient.Dispose();
                broadcastClient = null;
            }
            if (realtimeUpdate != null) {
                realtimeUpdate = null;
            }
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
        #endregion

    }


}