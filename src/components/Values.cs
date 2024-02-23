using System;
using System.Diagnostics;

using GameReaderCommon;

using ksBroadcastingNetwork;

using SimHub.Plugins;


namespace KLPlugins.RaceEngineer {

    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    public class Values : IDisposable {
        public Booleans.Booleans Booleans = new Booleans.Booleans();
        public Car.Car Car = new Car.Car();
        public Track.Track Track = new Track.Track();
        public Laps.Laps Laps = new Laps.Laps();
        public Weather Weather = new Weather();
        //public ACCRawData RawData = new ACCRawData();
        public Session Session = new Session();
        public Remaining.RemainingInSession RemainingInSession = new Remaining.RemainingInSession();
        public Remaining.RemainingOnFuel RemainingOnFuel = new Remaining.RemainingOnFuel();
        public Database.Database Db = new Database.Database();

        //public ACCUdpRemoteClient BroadcastClient = null;

        public Values() { }

        public void Reset() {
            //if (BroadcastClient != null) {
            //    DisposeBroadcastClient();
            //}

            this.Booleans.Reset();
            this.Car.Reset();
            this.Track.Reset();
            this.Laps.Reset();
            this.Weather.Reset();
            this.RemainingInSession.Reset();
            this.RemainingOnFuel.Reset();
            // RawData.Reset();
            this.Session.Reset();
        }


        #region IDisposable Support
        ~Values() {
            this.Dispose(false);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed = false;
        protected virtual void Dispose(bool disposing) {
            if (!this.isDisposed) {
                if (disposing) {
                    RaceEngineerPlugin.LogInfo("Disposed");
                    this.Db.Dispose();
                    //DisposeBroadcastClient();
                }

                this.isDisposed = true;
            }
        }

        public void Dispose() {
            this.Dispose(true);
        }
        #endregion

        private void OnNewStint(GameData data) {
            this.Laps.OnNewStint();
            this.Car.OnNewStint();
            this.Db.InsertStint(data, this);
        }

        public void OnGameStateChanged(bool running, PluginManager manager) {
            if (running) {
                //if (BroadcastClient != null) {
                //    RaceEngineerPlugin.LogWarn("Broadcast client wasn't 'null' at start of new event. Shouldn't be possible, there is a bug in disposing of Broadcast client from previous session.");
                //    DisposeBroadcastClient();
                //}
                //ConnectToBroadcastClient();
            } else {
                this.Reset();
            }

        }

        public void OnNewEvent(GameData data) {
            RaceEngineerPlugin.LogInfo($"OnNewEvent.");
            var sessType = Helpers.RaceSessionTypeFromString(data.NewData.SessionTypeName);
            this.Booleans.OnNewEvent(sessType);
            this.Track.OnNewEvent(data);
            this.Car.OnNewEvent(data, this);
            this.Laps.OnNewEvent(data, this);
            this.Db.InsertEvent(data, this);
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
            // RawData.Update((SHACCRawData)data.NewData.GetRawDataObject());

            if (this.Booleans.NewData.IsNewEvent) {
                RaceEngineerPlugin.LogFileSeparator();
                this.OnNewEvent(data);
            }

            this.Booleans.OnRegularUpdate(data, this);
            this.Session.OnRegularUpdate(data, this);
            this.Track.OnRegularUpdate(data);
            this.Car.OnRegularUpdate(data, this);
            this.Weather.OnRegularUpdate(data, this);

            if (this.Booleans.NewData.ExitedPitLane && !this.Booleans.NewData.IsInMenu) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New stint on pit exit.");
                this.OnNewStint(data);
            }

            // We need to add stint at the start of the race/hotlap/hotstint separately since we are never in pitlane.
            if (!this.Booleans.NewData.IsRaceStartStintAdded && this.Booleans.NewData.IsMoving && (this.Session.RaceSessionType == RaceSessionType.Race || this.Session.RaceSessionType == RaceSessionType.Hotstint || this.Session.RaceSessionType == RaceSessionType.Hotlap)) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New stint on race/hotlap/hotstint start.");
                this.OnNewStint(data);
                this.Booleans.RaceStartStintAdded();
            }

            this.RemainingInSession.OnRegularUpdate(data, this);
            this.RemainingOnFuel.OnRegularUpdate(this);

            if (this.Booleans.NewData.IsLapFinished) {
                Stopwatch sw = Stopwatch.StartNew();

                this.Booleans.OnLapFinished(data);
                this.Car.OnLapFinished(data, this);
                this.Laps.OnLapFinished(data, this);
                if (this.Laps.LastTime != 0) {
                    this.Db.InsertLap(data, this);
                }

                this.Weather.OnLapFinishedAfterInsert(data);
                this.Car.OnLapFinishedAfterInsert();

                var t = sw.Elapsed;
                RaceEngineerPlugin.LogInfo($"Lap finished. Update took {t.TotalMilliseconds}ms.");
                RaceEngineerPlugin.LogFileSeparator();
            }

            if (this.Session.IsNewSession) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New session");
                this.Booleans.OnNewSession(this);
                this.Session.OnNewSession();
                this.Car.OnNewSession(data, this);
                this.Laps.OnNewSession(data, this);
                this.Db.InsertSession(data, this);
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