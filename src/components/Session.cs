using GameReaderCommon;
using ksBroadcastingNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaceEngineerPlugin {
    public class Session {
        public RaceSessionType? RaceSessionType { get; private set; }
        public bool IsRaceSessionChange { get; private set; }

        public Session() {
            Reset();
        }

        public void Reset() {
            RaceSessionType = null;
            IsRaceSessionChange = false;
        }

        public void OnRegularUpdate(GameData data, Values v) {
            var newSessType = v.RawData.NewData.Realtime?.SessionType ?? Helpers.RaceSessionTypeFromString(data.NewData.SessionTypeName);
            IsRaceSessionChange = newSessType != RaceSessionType;
            RaceSessionType = newSessType;
        }

    }
}
