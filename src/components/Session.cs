using System;
using System.Diagnostics;

using ACSharedMemory.ACC.Reader;

using GameReaderCommon;

namespace KLPlugins.RaceEngineer {
    public class Session {
        public SessionType SessionType { get; private set; } = SessionType.Unknown;
        public bool IsNewSession { get; private set; }
        public int TimeMultiplier { get; private set; }
        public double TimeOfDay { get; private set; }


        private double _startClock = double.NaN;
        private double _startISplit = double.NaN;
        private double _firstClock = double.NaN;
        private bool _isTimeMultiplierCalculated = false;

        public Session() {
            this.Reset();
        }

        public void Reset() {
            this.SessionType = SessionType.Unknown;
            this.IsNewSession = false;
            this.TimeMultiplier = -1;
            this.TimeOfDay = 0;
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
            this.TimeOfDay = 0;
        }

        public void OnRegularUpdate(GameData data, Values v) {
            var newSessType = SessionTypeMethods.FromSHGameData(data);
            this.IsNewSession = newSessType != this.SessionType;
            this.SessionType = newSessType;

            if (RaceEngineerPlugin.Game.IsAcc) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();

                this.TimeOfDay = rawDataNew.Graphics.clock;

                if (!this._isTimeMultiplierCalculated) {
                    this.SetTimeMultiplier(v, rawDataNew);
                }
            }

        }

        /// <summary>
        /// Assumes that the game is ACC.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="rawDataNew"></param>
        private void SetTimeMultiplier(Values v, ACCRawData rawDataNew) {
            Debug.Assert(RaceEngineerPlugin.Game.IsAcc);

            if (double.IsNaN(this._firstClock) && rawDataNew.Graphics.iSplit > 5.0 && rawDataNew.Graphics.clock != 0.0) {
                this._firstClock = rawDataNew.Graphics.clock;

                RaceEngineerPlugin.LogInfo($"_firstClock = {this._firstClock}");
            }

            if (double.IsNaN(this._firstClock)) return;

            if (double.IsNaN(this._startClock) && rawDataNew.Graphics.clock - this._firstClock > 5.0 && rawDataNew.Graphics.iSplit > 5.0) {
                this._startClock = rawDataNew.Graphics.clock;
                this._startISplit = rawDataNew.Graphics.iSplit;
                RaceEngineerPlugin.LogInfo($"Started timer. _startClock = {this._startClock}, _startIsplit = {this._startISplit}");
            }

            var diffMs = rawDataNew.Graphics.iSplit - this._startISplit;
            if (diffMs > 5000) {
                this.TimeMultiplier = (int)Math.Round((rawDataNew.Graphics.clock - this._startClock) / (diffMs) * 1000.0);
                this._isTimeMultiplierCalculated = true;
                v.Db.UpdateSessionTimeMultiplier(this.TimeMultiplier);
                RaceEngineerPlugin.LogInfo($"Ended timer. TimeMultiplier = {this.TimeMultiplier}");
            }
        }
    }

    public enum SessionType {
        Practice,
        Qualifying,
        Superpole,
        Race,
        Hotlap,
        Hotstint,
        HotlapSuperpole,
        Drift,
        TimeAttack,
        Drag,
        Warmup,
        TimeTrial,
        Unknown
    }

    public static class SessionTypeMethods {
        public static SessionType FromSHGameData(GameData data) {
            if (RaceEngineerPlugin.Game.IsAcc) {
                var accData = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();
                return accData.Graphics.Session switch {
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_UNKNOWN => SessionType.Unknown,
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_PRACTICE => SessionType.Practice,
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_QUALIFY => SessionType.Qualifying,
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_RACE => SessionType.Race,
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_HOTLAP => SessionType.Hotlap,
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_TIME_ATTACK => SessionType.TimeAttack,
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_DRIFT => SessionType.Drift,
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_DRAG => SessionType.Drag,
                    (ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE)7 => SessionType.Hotstint,
                    (ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE)8 => SessionType.HotlapSuperpole,
                    _ => SessionType.Unknown,
                };
            } else if (RaceEngineerPlugin.Game.IsAc) {
                var acData = (ACSharedMemory.Reader.ACRawData)data.NewData.GetRawDataObject();
                return acData.Graphics.Session switch {
                    ACSharedMemory.AC_SESSION_TYPE.AC_UNKNOWN => SessionType.Unknown,
                    ACSharedMemory.AC_SESSION_TYPE.AC_PRACTICE => SessionType.Practice,
                    ACSharedMemory.AC_SESSION_TYPE.AC_QUALIFY => SessionType.Qualifying,
                    ACSharedMemory.AC_SESSION_TYPE.AC_RACE => SessionType.Race,
                    ACSharedMemory.AC_SESSION_TYPE.AC_HOTLAP => SessionType.Hotlap,
                    ACSharedMemory.AC_SESSION_TYPE.AC_TIME_ATTACK => SessionType.TimeAttack,
                    ACSharedMemory.AC_SESSION_TYPE.AC_DRIFT => SessionType.Drift,
                    ACSharedMemory.AC_SESSION_TYPE.AC_DRAG => SessionType.Drag,
                    _ => SessionType.Unknown,
                };
            }

            return FromString(data.NewData.SessionTypeName);
        }


        private static SessionType FromString(string s) {
            if (RaceEngineerPlugin.Game.IsAcc) {
                switch (s.ToLower()) {
                    case "7":
                        return SessionType.Hotstint;
                    case "8":
                        return SessionType.HotlapSuperpole;
                    default:
                        break;
                }
            }


            return s.ToLower() switch {
                "practice"
                or "open practice" or "offline testing" // IRacing
                or "practice 1" or "practice 2" or "practice 3" or "short practice" // F120xx
                => SessionType.Practice,

                "qualify"
                or "open qualify" or "lone qualify" // IRacing
                or "qualifying 1" or "qualifying 2" or "qualifying 3" or "short qualifying" or "OSQ" // F120xx
                => SessionType.Qualifying,

                "race"
                or "race 1" or "race 2" or "race 3" // F120xx
                => SessionType.Race,
                "hotlap" => SessionType.Hotlap,
                "hotstint" => SessionType.Hotstint,
                "hotlapsuperpole" => SessionType.HotlapSuperpole,
                "drift" => SessionType.Drift,
                "time_attack" => SessionType.TimeAttack,
                "drag" => SessionType.Drag,
                "time_trial" => SessionType.TimeTrial,
                "warmup" => SessionType.Warmup,
                _ => SessionType.Unknown,
            };
        }

        public static string ToPrettyString(SessionType s) {
            return s switch {
                SessionType.Practice => "Practice",
                SessionType.Qualifying => "Qualifying",
                SessionType.Race => "Race",
                SessionType.Hotlap => "Hotlap",
                SessionType.Hotstint => "Hotstint",
                SessionType.HotlapSuperpole => "Superpole",
                SessionType.Drift => "Drift",
                SessionType.Drag => "Drag",
                SessionType.TimeAttack => "Time attack",
                SessionType.TimeTrial => "Time trial",
                SessionType.Warmup => "Warmup",
                _ => "Unknown",
            };
        }
    }
}