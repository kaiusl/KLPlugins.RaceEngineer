using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using RaceEngineerPlugin.RawData;
using SHACCRawData = ACSharedMemory.ACC.Reader.ACCRawData;
using ksBroadcastingNetwork;
using ACCUdpRemoteClient = RaceEngineerPlugin.ksBroadcastingNetwork.ACCUdpRemoteClient;

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

        public ACCUdpRemoteClient broadcastClient;

        public Remaining.RemainingInSession remainingInSession = new Remaining.RemainingInSession();
        public Remaining.RemainingOnFuel remainingOnFuel = new Remaining.RemainingOnFuel();

        public Database.Database db = new Database.Database();

        public Values() {}

        public void Reset() {
            if (broadcastClient != null) {
                DisposeBroadcastClient();
            }

            booleans.Reset();
            car.Reset();
            track.Reset();
            laps.Reset();
            weather.Reset();
            remainingInSession.Reset();
            remainingOnFuel.Reset();
            RawData.Reset();
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

        private void OnNewStint(GameData data) {
            laps.OnNewStint();
            car.OnNewStint();
            db.InsertStint(data, this);
        }

        public void OnGameStateChanged(bool running) {
            if (running) {
                if (broadcastClient != null) {
                    RaceEngineerPlugin.LogWarn("Broadcast client wasn't 'null' at start of new event. Shouldn't be possible, there is a bug in disposing of Broadcast client from previous session.");
                    DisposeBroadcastClient();
                }
                ConnectToBroadcastClient();
            }

        }

        public void OnNewEvent(GameData data) {
            RaceEngineerPlugin.LogInfo($"OnNewEvent. {data.NewData}");

            //var sessTypeStr = data.NewData.SessionTypeName;
            //RaceSessionType sessType;
            //switch (sessTypeStr) {
            //    case "RACE":
            //        sessType = RaceSessionType.Race;
            //        break;
            //    case ""
            //}       


            booleans.OnNewEvent(RawData.NewData.Realtime.SessionType);
            track.OnNewEvent(data);
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

            RawData.UpdateSharedMem((SHACCRawData)data.NewData.GetRawDataObject());

            if (booleans.NewData.IsNewEvent) {
                RaceEngineerPlugin.LogFileSeparator();
                OnNewEvent(data);
            }

            booleans.OnRegularUpdate(data, this);
            track.OnRegularUpdate(data);
            car.OnRegularUpdate(data, this);
            weather.OnRegularUpdate(data, this);


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

            remainingInSession.OnRegularUpdate(data, this);
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
                car.OnNewSession(this);
                laps.OnNewSession(this);
            }
        }

        public void OnGameNotRunning() {
            Reset();
        }

        #region Broadcast client connection
        public void ConnectToBroadcastClient() {
            broadcastClient = new ACCUdpRemoteClient("127.0.0.1", 9000, "REPlugin", "asd", "", 1000);
            broadcastClient.MessageHandler.OnRealtimeUpdate += RawData.OnBroadcastRealtimeUpdate;
            broadcastClient.MessageHandler.OnConnectionStateChanged += OnBroadcastConnectionStateChanged;
        }

        public void DisposeBroadcastClient() {
            if (broadcastClient != null) {
                broadcastClient.Shutdown();
                broadcastClient.Dispose();
                broadcastClient = null;
            }
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