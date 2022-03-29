using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using RaceEngineerPlugin.RawData;
using SHACCRawData = ACSharedMemory.ACC.Reader.ACCRawData;
using ksBroadcastingNetwork;
using System.Collections.Concurrent;

namespace RaceEngineerPlugin {

    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    public class Values : IDisposable {
        public Booleans.Booleans Booleans = new Booleans.Booleans();
        public Car.Car Car = new Car.Car();
        public Track.Track Track = new Track.Track();
        public Laps.Laps Laps = new Laps.Laps();
        public Weather Weather = new Weather();
        public ACCRawData RawData = new ACCRawData();
        public Session Session = new Session();
        public Remaining.RemainingInSession RemainingInSession = new Remaining.RemainingInSession();
        public Remaining.RemainingOnFuel RemainingOnFuel = new Remaining.RemainingOnFuel();
        public Database.Database Db = new Database.Database();

        //public ACCUdpRemoteClient BroadcastClient = null;

        public Values() {}

        public void Reset() {
            //if (BroadcastClient != null) {
            //    DisposeBroadcastClient();
            //}

            Booleans.Reset();
            Car.Reset();
            Track.Reset();
            Laps.Reset();
            Weather.Reset();
            RemainingInSession.Reset();
            RemainingOnFuel.Reset();
            RawData.Reset();
            Session.Reset();
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
                    Db.Dispose();
                    //DisposeBroadcastClient();
                }

                isDisposed = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion

        private void OnNewStint(GameData data) {
            Laps.OnNewStint();
            Car.OnNewStint();
            Db.InsertStint(data, this);
        }

        public void OnGameStateChanged(bool running, PluginManager manager) {
            if (running) {
                //if (BroadcastClient != null) {
                //    RaceEngineerPlugin.LogWarn("Broadcast client wasn't 'null' at start of new event. Shouldn't be possible, there is a bug in disposing of Broadcast client from previous session.");
                //    DisposeBroadcastClient();
                //}
                //ConnectToBroadcastClient();
            } else {
                Reset();
            }

        }

        public void OnNewEvent(GameData data) {
            RaceEngineerPlugin.LogInfo($"OnNewEvent.");
            var sessType = RawData.NewData.Realtime?.SessionType ?? Helpers.RaceSessionTypeFromString(data.NewData.SessionTypeName);
            Booleans.OnNewEvent(sessType);
            Track.OnNewEvent(data);
            Car.OnNewEvent(data, this);
            Laps.OnNewEvent(this);
            Db.InsertEvent(data, this);
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
        private DateTime lastWeather = DateTime.Now;
        public void OnDataUpdate(GameData data) {
            RawData.Update((SHACCRawData)data.NewData.GetRawDataObject());

            if (Booleans.NewData.IsNewEvent) {
                RaceEngineerPlugin.LogFileSeparator();
                OnNewEvent(data);
            }

            Booleans.OnRegularUpdate(data, this);
            Session.OnRegularUpdate(data, this);
            Track.OnRegularUpdate(data);
            Car.OnRegularUpdate(data, this);
            Weather.OnRegularUpdate(data, this);

            if (Booleans.NewData.ExitedPitLane && !Booleans.NewData.IsInMenu) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New stint on pit exit.");
                OnNewStint(data);
            }

            // We need to add stint at the start of the race/hotlap/hotstint separately since we are never in pitlane.
            if (!Booleans.NewData.IsRaceStartStintAdded && Booleans.NewData.IsMoving && (Session.RaceSessionType == RaceSessionType.Race || Session.RaceSessionType == RaceSessionType.Hotstint || Session.RaceSessionType == RaceSessionType.Hotlap)) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New stint on race/hotlap/hotstint start.");
                OnNewStint(data);
                Booleans.RaceStartStintAdded();
            }

            RemainingInSession.OnRegularUpdate(data, this);
            RemainingOnFuel.OnRegularUpdate(this);

            if (Booleans.NewData.IsLapFinished) {
                Stopwatch sw = Stopwatch.StartNew();

                Booleans.OnLapFinished(data);
                Car.OnLapFinished(data, this);
                Laps.OnLapFinished(data, this);
                if (Laps.LastTime != 0) {
                    Db.InsertLap(data, this);
                }

                Weather.OnLapFinishedAfterInsert(data);
                Car.OnLapFinishedAfterInsert();

                var t = sw.Elapsed;
                RaceEngineerPlugin.LogInfo($"Lap finished. Update took {t.TotalMilliseconds}ms.");
                RaceEngineerPlugin.LogFileSeparator();
            }

            if (Session.IsNewSession) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New session");
                Booleans.OnNewSession(this);
                Session.OnNewSession();
                Car.OnNewSession(this);
                Laps.OnNewSession(this);
                Db.InsertSession(data, this);
            }

            //if ((data.NewData.PacketTime - lastWeather).TotalSeconds > 5) {
            //    File.AppendAllText($"{RaceEngineerPlugin.Settings.DataLocation}\\weather.txt", $"{data.NewData.PacketTime.Ticks}; {RawData.NewData.Graphics.clock}; {data.NewData.AirTemperature}; {data.NewData.RoadTemperature}; {RawData.NewData.Graphics.rainIntensity}; {RawData.NewData.Graphics.rainIntensityIn10min}; {RawData.NewData.Graphics.rainIntensityIn30min}; {RawData.NewData.Graphics.trackGripStatus}; {RawData.NewData.Graphics.CarCount}; {RawData.NewData.Graphics.WindSpeed}\n");
            //    lastWeather = data.NewData.PacketTime;
            //}


        }

        #region Broadcast client connection

        //public void ConnectToBroadcastClient() {
        //    BroadcastClient = new ACCUdpRemoteClient("127.0.0.1", 9000, "REPlugin", "asd", "", 100);
        //    BroadcastClient.MessageHandler.OnRealtimeUpdate += RawData.OnBroadcastRealtimeUpdate;
        //    BroadcastClient.MessageHandler.OnConnectionStateChanged += OnBroadcastConnectionStateChanged;
        //}

        //public void DisposeBroadcastClient() {
        //    if (BroadcastClient != null) {
        //        BroadcastClient.Shutdown();
        //        BroadcastClient.Dispose();
        //        BroadcastClient = null;
        //    }
        //}

        //private void OnBroadcastConnectionStateChanged(int connectionId, bool connectionSuccess, bool isReadonly, string error) {
        //    if (connectionSuccess) {
        //        RaceEngineerPlugin.LogInfo("Connected to broadcast client.");
        //    } else {
        //        RaceEngineerPlugin.LogWarn($"Failed to connect to broadcast client. Err: {error}");
        //    }
        //}

        #endregion

    }


}