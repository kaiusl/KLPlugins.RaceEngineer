using GameReaderCommon;

using ksBroadcastingNetwork;

namespace KLPlugins.RaceEngineer.Booleans {

    /// <summary>
    /// Hold single set of boolean values
    /// </summary>
    public class BooleansBase {
        public bool IsInMenu { get; private set; }
        public bool EnteredMenu { get; private set; }
        public bool ExitedMenu { get; private set; }

        public bool IsInPitLane { get; private set; }
        public bool EnteredPitLane { get; private set; }
        public bool ExitedPitLane { get; private set; }

        public bool IsInPitBox { get; private set; }
        public bool EnteredPitBox { get; private set; }
        public bool ExitedPitBox { get; private set; }

        public bool IsOnTrack { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsLapFinished { get; private set; }

        public bool IsSetupMenuVisible { get; private set; }
        public bool IsFuelWarning { get; private set; }
        public bool HavePressuresChanged { get; private set; }

        public bool HasNewStintStarted { get; private set; }
        public bool IsValidFuelLap { get; private set; }
        public bool IsTimeLimitedSession { get; private set; }

        public bool IsLapLimitedSession { get; private set; }
        public bool SavePrevLap { get; private set; }
        public bool HasSetupChanged { get; private set; }

        public bool IsNewEvent { get; private set; }
        public bool IsRaceStartStintAdded { get; private set; }
        public bool IsOutLap { get; private set; }

        public bool IsInLap { get; private set; }
        public bool EcuMapChangedThisLap { get; private set; }
        public bool RainIntensityChangedThisLap { get; private set; }


        private bool _isSessionLimitSet = false;

        public BooleansBase() {
            this.Reset();
        }

        public void Update(BooleansBase o) {
            this.IsInMenu = o.IsInMenu;
            this.EnteredMenu = o.EnteredMenu;
            this.ExitedMenu = o.ExitedMenu;

            this.IsInPitLane = o.IsInPitLane;
            this.ExitedPitLane = o.ExitedPitLane;
            this.EnteredPitLane = o.EnteredPitLane;

            this.IsInPitBox = o.IsInPitBox;
            this.ExitedPitBox = o.ExitedPitBox;
            this.EnteredPitBox = o.EnteredPitBox;

            this.IsOnTrack = o.IsOnTrack;
            this.IsMoving = o.IsMoving;
            this.IsLapFinished = o.IsLapFinished;

            this.IsSetupMenuVisible = o.IsSetupMenuVisible;
            this.IsFuelWarning = o.IsFuelWarning;
            this.HavePressuresChanged = o.HavePressuresChanged;

            this.HasNewStintStarted = o.HasNewStintStarted;
            this.IsValidFuelLap = o.IsValidFuelLap;
            this.IsTimeLimitedSession = o.IsTimeLimitedSession;

            this.IsLapLimitedSession = o.IsLapLimitedSession;
            this.SavePrevLap = o.SavePrevLap;
            this.HasSetupChanged = o.HasSetupChanged;

            this.IsNewEvent = o.IsNewEvent;
            this.IsRaceStartStintAdded = o.IsRaceStartStintAdded;
            this.IsOutLap = o.IsOutLap;

            this.IsInLap = o.IsInLap;
            this.EcuMapChangedThisLap = o.EcuMapChangedThisLap;
            this.RainIntensityChangedThisLap = o.RainIntensityChangedThisLap;
        }

        public void Update(GameData data, Values v) {
            this.IsInMenu = data.NewData.AirTemperature == 0;
            var wasInMenu = data.OldData.AirTemperature == 0;
            this.EnteredMenu = !wasInMenu && this.IsInMenu;
            this.ExitedMenu = wasInMenu && !this.IsInMenu;

            this.IsInPitLane = data.NewData.IsInPitLane == 1;
            this.IsInPitBox = data.NewData.IsInPit == 1;
            var wasInPitLane = data.OldData.IsInPitLane == 1;
            var wasInPitBox = data.OldData.IsInPit == 1;
            this.EnteredPitLane = !wasInPitLane && this.IsInPitLane;
            this.ExitedPitLane = wasInPitLane && !this.IsInPitLane;
            this.EnteredPitBox = !wasInPitBox && this.IsInPitBox;
            this.ExitedPitBox = wasInPitBox && !this.IsInPitBox;

            if (this.EnteredMenu) {
                RaceEngineerPlugin.LogInfo("Entered menu");
            }
            if (this.ExitedMenu) {
                RaceEngineerPlugin.LogInfo("Exited menu");
            }
            if (this.EnteredPitLane) {
                RaceEngineerPlugin.LogInfo("Entered pitlane");
            }
            if (this.ExitedPitLane) {
                RaceEngineerPlugin.LogInfo("Exited pitlane");
            }
            if (this.EnteredPitBox) {
                RaceEngineerPlugin.LogInfo("Entered pitbox");
            }
            if (this.ExitedPitBox) {
                RaceEngineerPlugin.LogInfo("Exited pitbox");
            }


            // In ACC AirTemp=0 if UI is visible. Nice way to identify but doesn't work in other games.
            this.IsOnTrack = !this.IsInPitLane && !data.GamePaused && (RaceEngineerPlugin.Game.IsAcc ? data.NewData.AirTemperature > 0.0 : true);
            if (RaceEngineerPlugin.Game.IsAcc && this.IsInMenu) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();
                this.IsSetupMenuVisible = rawDataNew.Graphics.IsSetupMenuVisible == 1;
            }

            this.IsMoving = data.NewData.SpeedKmh > 1;
            this.IsLapFinished = data.OldData.CompletedLaps < data.NewData.CompletedLaps;
            if (!this._isSessionLimitSet) {
                // Need to set once as at the end of the session SessionTimeLeft == 0 and this will confuse plugin
                this.IsLapLimitedSession = data.NewData.RemainingLaps > 0;
                this.IsTimeLimitedSession = !this.IsLapLimitedSession;

            }

            if (this.IsValidFuelLap && this.IsInPitLane) {
                RaceEngineerPlugin.LogInfo("Set 'IsValidFuelLap = false'");
                this.IsValidFuelLap = false;
            }
            // IsValidFuelLap &= !IsInPitLane;

            double lastLapTime = data.NewData.LastLapTime.TotalSeconds;

            if (this.IsLapFinished) {
                this.SavePrevLap = lastLapTime > 0.0
                    && this.IsValidFuelLap
                    && v.Car.Fuel.RemainingAtLapStart != 0.0
                    && v.Car.Fuel.RemainingAtLapStart > data.NewData.Fuel
                    && !this.IsInLap
                    && !this.IsOutLap;
                RaceEngineerPlugin.LogInfo($"'SaveLap = {this.SavePrevLap}', 'lastLapTime = {lastLapTime}', 'IsValidFuelLap = {this.IsValidFuelLap}', 'fuelUsedLapStart = {v.Car.Fuel.RemainingAtLapStart}', 'data.NewData.Fuel = {data.NewData.Fuel}'");
            }

            if (!this.IsInLap && (this.EnteredPitLane || this.EnteredMenu)) {
                if (this.IsMoving || (data.OldData.AirTemperature != 0 && data.NewData.AirTemperature == 0)) {
                    RaceEngineerPlugin.LogInfo("Set 'IsInLap = true'");
                    this.IsInLap = true;
                }
            }

            if (!this.IsOutLap && (this.ExitedPitLane || this.ExitedMenu)) {
                RaceEngineerPlugin.LogInfo("Set 'IsOutLap = true'");
                this.IsOutLap = true;
            }
            // IsOutLap |= ExitedPitLane;

            if (!this.EcuMapChangedThisLap && !this.IsInMenu && !this.IsInPitLane && data.OldData.EngineMap != data.NewData.EngineMap) {
                RaceEngineerPlugin.LogInfo("Set 'EcuMapChangedThisLap = true'");
                this.EcuMapChangedThisLap = true;
            }
            //EcuMapChangedThisLap |= (data.OldData.EngineMap != data.NewData.EngineMap);

            if (RaceEngineerPlugin.Game.IsAcc && !this.RainIntensityChangedThisLap && !this.IsInMenu) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();
                var rawDataOld = (ACSharedMemory.ACC.Reader.ACCRawData)data.OldData.GetRawDataObject();

                if (!this.IsInPitLane && rawDataNew.Graphics.rainIntensity != rawDataOld.Graphics.rainIntensity) {
                    RaceEngineerPlugin.LogInfo("Set 'RainIntensityChangedThisLap = true'");
                    this.RainIntensityChangedThisLap = true;
                }
            }

        }

        public void Reset(RaceSessionType sessionType = RaceSessionType.Practice) {
            this.IsInMenu = true;
            this.EnteredMenu = false;
            this.ExitedMenu = false;

            this.IsInPitLane = true;
            this.EnteredPitLane = false;
            this.ExitedPitLane = false;

            this.IsInPitBox = false;
            this.EnteredPitBox = false;
            this.ExitedPitBox = false;

            this.IsOnTrack = false;
            this.IsMoving = false;
            this.IsLapFinished = false;

            this.IsSetupMenuVisible = false;
            this.IsFuelWarning = false;
            this.HavePressuresChanged = false;

            this.HasNewStintStarted = false;
            this.IsValidFuelLap = false;
            this.IsTimeLimitedSession = false;

            this.IsLapLimitedSession = false;
            this.SavePrevLap = false;
            this.HasSetupChanged = false;

            this.IsNewEvent = true;
            this.IsRaceStartStintAdded = false;
            this.IsOutLap = !(sessionType == RaceSessionType.Hotstint || sessionType == RaceSessionType.Hotlap); // First lap of HOTSTINT/HOTLAP is proper lap.

            this.IsValidFuelLap = sessionType == RaceSessionType.Hotstint; // First lap of HOTSTINT is proper lap
            this.EcuMapChangedThisLap = false;
            this.RainIntensityChangedThisLap = false;

            this._isSessionLimitSet = false;
        }

        public void RaceStartStintAdded() {
            this.IsRaceStartStintAdded = true;
        }

        public void OnNewEvent(RaceSessionType sessionType) {
            this.Reset(sessionType);
            this.IsNewEvent = false;
        }

        public void OnSessionChange(RaceSessionType sessionType) {
            this.Reset(sessionType);
            this.IsNewEvent = false;
        }

        public void OnLapFinished(GameData data) {
            // HOTLAP doesn't have fuel usage, thus set isValidFuelLap = false in that case always, otherwise reset to valid lap in other cases
            this.IsValidFuelLap = data.NewData.SessionTypeName != "HOTLAP";
            this.IsOutLap = false;
            this.IsInLap = false;
            this.EcuMapChangedThisLap = false;
            RaceEngineerPlugin.LogInfo($@"Set 'IsValidFuelLap = {this.IsValidFuelLap}', 'IsOutLap = false', 'IsInLap = false'");
        }

    }


    /// <summary>
    /// Hold current and previous boolean values 
    /// </summary>
    public class Booleans {
        public BooleansBase NewData { get; private set; }
        public BooleansBase OldData { get; private set; }

        public Booleans() {
            this.NewData = new BooleansBase();
            this.OldData = new BooleansBase();
        }

        public void Reset(RaceSessionType sessionType = RaceSessionType.Practice) {
            RaceEngineerPlugin.LogInfo("Booleans.Reset()");
            this.OldData.Reset(sessionType);
            this.NewData.Reset(sessionType);
        }

        public void RaceStartStintAdded() {
            RaceEngineerPlugin.LogInfo("Booleans.RaceStartStintAdded()");
            this.NewData.RaceStartStintAdded();
        }

        public void OnNewEvent(RaceSessionType sessionType) {
            this.NewData.OnNewEvent(sessionType);
            this.OldData.OnNewEvent(sessionType);
        }

        public void OnNewSession(Values v) {
            this.NewData.OnSessionChange(v.Session.RaceSessionType ?? RaceSessionType.Practice);
        }

        public void OnLapFinished(GameData data) {
            this.NewData.OnLapFinished(data);
        }

        public void OnRegularUpdate(GameData data, Values v) {
            this.OldData.Update(this.NewData);
            this.NewData.Update(data, v);
        }

    }
}