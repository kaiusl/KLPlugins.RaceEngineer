using GameReaderCommon;
using ksBroadcastingNetwork;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaceEngineerPlugin {
    public class Session {
        public RaceSessionType? RaceSessionType { get; private set; }
        public bool IsNewSession { get; private set; }
        public int TimeMultiplier { get; private set; }

        private double _startClock = double.NaN;
        private double _startISplit = double.NaN;
        private double _firstClock = double.NaN;
        private bool _isTimeMultiplierCalculated = false;

        public Session() {
            Reset();
        }

        public void Reset() {
            RaceSessionType = null;
            IsNewSession = false;
            TimeMultiplier = -1;
            _startClock = double.NaN;
            _startISplit = double.NaN;
            _firstClock = double.NaN;
            _isTimeMultiplierCalculated = false;
        }

        public void OnNewSession() {
            TimeMultiplier = -1;
            _startClock = double.NaN;
            _startISplit = double.NaN;
            _firstClock = double.NaN;
            _isTimeMultiplierCalculated = false;
        }

        public void OnRegularUpdate(GameData data, Values v) {
            var newSessType = v.RawData.NewData.Realtime?.SessionType ?? Helpers.RaceSessionTypeFromString(data.NewData.SessionTypeName);
            IsNewSession = newSessType != RaceSessionType;
            RaceSessionType = newSessType;

            if (!_isTimeMultiplierCalculated) {
                if (double.IsNaN(_firstClock) && v.RawData.NewData.Graphics.iSplit > 5.0 && v.RawData.NewData.Graphics.clock != 0.0) {
                    _firstClock = v.RawData.NewData.Graphics.clock;

                    RaceEngineerPlugin.LogInfo($"_firstClock = {_firstClock}");
                }

                if (double.IsNaN(_firstClock)) return;

                if (double.IsNaN(_startClock) && v.RawData.NewData.Graphics.clock - _firstClock > 5.0 && v.RawData.NewData.Graphics.iSplit > 5.0) {
                    _startClock = v.RawData.NewData.Graphics.clock;
                    _startISplit = v.RawData.NewData.Graphics.iSplit;
                    RaceEngineerPlugin.LogInfo($"Started timer. _startClock = {_startClock}, _startIsplit = {_startISplit}");
                }

                var diffMs = v.RawData.NewData.Graphics.iSplit - _startISplit;
                if (diffMs > 5000) {
                    TimeMultiplier = (int)Math.Round((v.RawData.NewData.Graphics.clock - _startClock) / (diffMs) * 1000.0);
                    _isTimeMultiplierCalculated = true;
                    v.Db.UpdateSessionTimeMultiplier(TimeMultiplier);
                    RaceEngineerPlugin.LogInfo($"Ended timer. TimeMultiplier = {TimeMultiplier}");
                }
            }

        }

    }
}
