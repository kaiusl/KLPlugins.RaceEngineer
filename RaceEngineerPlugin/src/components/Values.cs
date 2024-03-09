using System;
using System.Diagnostics;

using GameReaderCommon;

using SimHub.Plugins;

namespace KLPlugins.RaceEngineer {

    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    public sealed class Values {
        // NOTE: It's important to never reassign these values. 
        // The property exports to SimHub rely on the fact that they point to one place always.
        public Booleans.Booleans Booleans { get; } = new();
        public Car.Car Car { get; } = new();
        public Track.Track Track { get; } = new();
        public Laps.Laps Laps { get; } = new();
        public Weather Weather { get; } = new();
        public Session Session { get; } = new();
        public Remaining.RemainingInSession RemainingInSession { get; } = new();
        public Remaining.RemainingOnFuel RemainingOnFuel { get; } = new();
        internal Database.Database Db { get; } = new();

        public delegate void NewXXXStartedEventHandler(Values sender, GameData data);
        public event NewXXXStartedEventHandler? NewEventStarted;
        public event NewXXXStartedEventHandler? NewStintStarted;
        public event NewXXXStartedEventHandler? NewSessionStarted;
        public event NewXXXStartedEventHandler? LapFinished;

        internal Values(PluginManager pm) {
            pm.GameStateChanged += this.OnGameStateChanged;
        }

        private void Reset() {
            this.Booleans.Reset();
            this.Car.Reset();
            this.Track.Reset();
            this.Laps.Reset();
            this.Weather.Reset();
            this.RemainingInSession.Reset();
            this.RemainingOnFuel.Reset();
            this.Session.Reset();
        }


        #region IDisposable Support
        ~Values() {
            this.Dispose(false);
        }

        private bool _isDisposed = false;
        private void Dispose(bool disposing) {
            if (!this._isDisposed) {
                if (disposing) {
                    this.Db.Dispose();
                    //DisposeBroadcastClient();
                    RaceEngineerPlugin.LogInfo("Disposed");
                }

                this._isDisposed = true;
            }
        }

        internal void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        private void OnNewStint(GameData data) {
            this.Laps.OnNewStint();
            this.Car.OnNewStint();
            this.Db.InsertStint(data, this);
        }

        private void OnGameStateChanged(bool running, PluginManager manager) {
            if (running) {
                //
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
        internal void OnDataUpdate(GameData data) {
            // RawData.Update((SHACCRawData)data.NewData.GetRawDataObject());

            var isNewEvent = this.Booleans.NewData.IsNewEvent;
            if (isNewEvent) {
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
                this.OnNewStint(data);
                isNewStint = true;
            }

            // We need to add stint at the start of the race/hotlap/hotstint separately since we are never in pitlane.
            if (
                !this.Booleans.NewData.IsRaceStartStintAdded
                && this.Booleans.NewData.IsMoving
                && this.Session.SessionType is SessionType.Race or SessionType.Hotstint or SessionType.Hotlap) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New stint on race/hotlap/hotstint start.");
                this.OnNewStint(data);
                this.Booleans.RaceStartStintAdded();
                isNewStint = true;
            }

            this.RemainingInSession.OnRegularUpdate(data, this);
            this.RemainingOnFuel.OnRegularUpdate(this);

            var isLapFinished = this.Booleans.NewData.IsLapFinished;
            if (isLapFinished) {
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

            var isNewSession = this.Session.IsNewSession;
            if (isNewSession) {
                RaceEngineerPlugin.LogFileSeparator();
                RaceEngineerPlugin.LogInfo("New session");
                this.Booleans.OnNewSession(this);
                this.Session.OnNewSession();
                this.Car.OnNewSession(data, this);
                this.Laps.OnNewSession(data, this);
                this.Db.InsertSession(data, this);
            }

            // We want to invoke these after all the data has been updated from our side
            this.InvokeEvents(
                data,
                isNewEvent: isNewEvent,
                isNewStint: isNewStint,
                isNewSession: isNewSession,
                isLapFinished: isLapFinished
            );
        }

        private void InvokeEvents(GameData data, bool isNewEvent, bool isNewStint, bool isNewSession, bool isLapFinished) {
            if (this.NewEventStarted != null && isNewEvent) {
                this.NewEventStarted.Invoke(this, data);
            }

            if (this.NewStintStarted != null && isNewStint) {
                this.NewStintStarted.Invoke(this, data);
            }

            if (this.LapFinished != null && isLapFinished) {
                this.LapFinished.Invoke(this, data);
            }

            if (this.NewSessionStarted != null && isNewSession) {
                this.NewSessionStarted.Invoke(this, data);
            }
        }
    }
}