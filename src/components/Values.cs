﻿using System;
using System.Diagnostics;

using GameReaderCommon;

using SimHub.Plugins;


namespace KLPlugins.RaceEngineer {

    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    public class Values : IDisposable {
        public Booleans.Booleans Booleans = new();
        public Car.Car Car = new();
        public Track.Track Track = new();
        public Laps.Laps Laps = new();
        public Weather Weather = new();
        //public ACCRawData RawData = new ACCRawData();
        public Session Session = new();
        public Remaining.RemainingInSession RemainingInSession = new();
        public Remaining.RemainingOnFuel RemainingOnFuel = new();
        internal Database.Database Db = new();

        public delegate void OnNewEventHandler(in GameData data, in Values v);
        public event OnNewEventHandler? OnNewEvent;
        public event OnNewEventHandler? OnNewStint;
        public event OnNewEventHandler? OnNewSession;
        public event OnNewEventHandler? OnLapFinished;

        //public ACCUdpRemoteClient BroadcastClient = null;

        public Values() { }

        internal void Reset() {
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

        private bool _isDisposed = false;
        protected virtual void Dispose(bool disposing) {
            if (!this._isDisposed) {
                if (disposing) {
                    this.Db.Dispose();
                    //DisposeBroadcastClient();
                    RaceEngineerPlugin.LogInfo("Disposed");
                }

                this._isDisposed = true;
            }
        }

        public void Dispose() {
            this.Dispose(true);
        }
        #endregion

        private void _onNewStint(GameData data) {
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
        private readonly DateTime lastWeather = DateTime.Now;
        public void OnDataUpdate(GameData data) {
            // RawData.Update((SHACCRawData)data.NewData.GetRawDataObject());

            if (this.Booleans.NewData.IsNewEvent) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo($"OnNewEvent.");
                var sessType = SessionTypeMethods.FromSHGameData(data);
                this.Booleans.OnNewEvent(sessType);
                this.Track.OnNewEvent(data);
                this.Car.OnNewEvent(data, this);
                this.Laps.OnNewEvent(data, this);
                this.Db.InsertEvent(data, this);
            }

            this.Booleans.OnRegularUpdate(data, this);
            this.Session.OnRegularUpdate(data, this);
            this.Track.OnRegularUpdate(data);
            this.Car.OnRegularUpdate(data, this);
            this.Weather.OnRegularUpdate(data, this);

            var isNewStint = false;
            if (this.Booleans.NewData.ExitedPitLane && !this.Booleans.NewData.IsInMenu) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New stint on pit exit.");
                this._onNewStint(data);
                isNewStint = true;
            }

            // We need to add stint at the start of the race/hotlap/hotstint separately since we are never in pitlane.
            if (
                !this.Booleans.NewData.IsRaceStartStintAdded
                && this.Booleans.NewData.IsMoving
                && this.Session.SessionType is SessionType.Race or SessionType.Hotstint or SessionType.Hotlap) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New stint on race/hotlap/hotstint start.");
                this._onNewStint(data);
                this.Booleans.RaceStartStintAdded();
                isNewStint = true;
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

            // We want to invoke these after all the data has been updated from our side
            this.InvokeEvents(data, isNewStint);
        }

        private void InvokeEvents(GameData data, bool isNewStint) {
            if (this.OnNewEvent != null && this.Booleans.NewData.IsNewEvent) {
                this.OnNewEvent.Invoke(data, this);
            }

            if (this.OnNewStint != null && isNewStint) {
                this.OnNewStint.Invoke(data, this);
            }

            if (this.OnLapFinished != null && this.Booleans.NewData.IsLapFinished) {
                this.OnLapFinished.Invoke(data, this);
            }

            if (this.OnNewSession != null && this.Session.IsNewSession) {
                this.OnNewSession.Invoke(data, this);
            }
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