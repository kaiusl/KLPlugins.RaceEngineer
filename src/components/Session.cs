using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GameReaderCommon;

using ksBroadcastingNetwork;

namespace KLPlugins.RaceEngineer {
    public class Session {
        public RaceSessionType? RaceSessionType { get; private set; }
        public bool IsNewSession { get; private set; }
        public int TimeMultiplier { get; private set; }

        private double _startClock = double.NaN;
        private double _startISplit = double.NaN;
        private double _firstClock = double.NaN;
        private bool _isTimeMultiplierCalculated = false;

        public Session() {
            this.Reset();
        }

        public void Reset() {
            this.RaceSessionType = null;
            this.IsNewSession = false;
            this.TimeMultiplier = -1;
            this._startClock = double.NaN;
            this._startISplit = double.NaN;
            this._firstClock = double.NaN;
            this._isTimeMultiplierCalculated = false;
        }

        public void OnNewSession() {
            this.TimeMultiplier = -1;
            this._startClock = double.NaN;
            this._startISplit = double.NaN;
            this._firstClock = double.NaN;
            this._isTimeMultiplierCalculated = false;
        }

        public void OnRegularUpdate(GameData data, Values v) {
            var newSessType = v.RawData.NewData.Realtime?.SessionType ?? Helpers.RaceSessionTypeFromString(data.NewData.SessionTypeName);
            this.IsNewSession = newSessType != this.RaceSessionType;
            this.RaceSessionType = newSessType;

            if (!this._isTimeMultiplierCalculated) {
                if (double.IsNaN(this._firstClock) && v.RawData.NewData.Graphics.iSplit > 5.0 && v.RawData.NewData.Graphics.clock != 0.0) {
                    this._firstClock = v.RawData.NewData.Graphics.clock;

                    RaceEngineerPlugin.LogInfo($"_firstClock = {this._firstClock}");
                }

                if (double.IsNaN(this._firstClock)) return;

                if (double.IsNaN(this._startClock) && v.RawData.NewData.Graphics.clock - this._firstClock > 5.0 && v.RawData.NewData.Graphics.iSplit > 5.0) {
                    this._startClock = v.RawData.NewData.Graphics.clock;
                    this._startISplit = v.RawData.NewData.Graphics.iSplit;
                    RaceEngineerPlugin.LogInfo($"Started timer. _startClock = {this._startClock}, _startIsplit = {this._startISplit}");
                }

                var diffMs = v.RawData.NewData.Graphics.iSplit - this._startISplit;
                if (diffMs > 5000) {
                    this.TimeMultiplier = (int)Math.Round((v.RawData.NewData.Graphics.clock - this._startClock) / (diffMs) * 1000.0);
                    this._isTimeMultiplierCalculated = true;
                    v.Db.UpdateSessionTimeMultiplier(this.TimeMultiplier);
                    RaceEngineerPlugin.LogInfo($"Ended timer. TimeMultiplier = {this.TimeMultiplier}");
                }
            }

        }

    }
}