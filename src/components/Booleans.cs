using GameReaderCommon;
using SimHub.Plugins;
using System.Diagnostics;
using System.IO;
using RaceEngineerPlugin.RawData;
using ksBroadcastingNetwork;

namespace RaceEngineerPlugin.Booleans {

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
            Reset();
        }

        public void Update(BooleansBase o) {
            IsInMenu = o.IsInMenu;
            EnteredMenu = o.EnteredMenu;
            ExitedMenu = o.ExitedMenu;

            IsInPitLane = o.IsInPitLane;
            ExitedPitLane = o.ExitedPitLane;
            EnteredPitLane = o.EnteredPitLane;

            IsInPitBox = o.IsInPitBox;
            ExitedPitBox = o.ExitedPitBox;
            EnteredPitBox = o.EnteredPitBox;

            IsOnTrack = o.IsOnTrack;
            IsMoving = o.IsMoving;
            IsLapFinished = o.IsLapFinished;

            IsSetupMenuVisible = o.IsSetupMenuVisible;
            IsFuelWarning = o.IsFuelWarning;
            HavePressuresChanged = o.HavePressuresChanged;

            HasNewStintStarted = o.HasNewStintStarted;
            IsValidFuelLap = o.IsValidFuelLap;
            IsTimeLimitedSession = o.IsTimeLimitedSession;

            IsLapLimitedSession = o.IsLapLimitedSession;
            SavePrevLap = o.SavePrevLap;
            HasSetupChanged = o.HasSetupChanged;

            IsNewEvent = o.IsNewEvent;
            IsRaceStartStintAdded = o.IsRaceStartStintAdded;
            IsOutLap = o.IsOutLap;

            IsInLap = o.IsInLap;
            EcuMapChangedThisLap = o.EcuMapChangedThisLap;
            RainIntensityChangedThisLap = o.RainIntensityChangedThisLap;
        }

        public void Update(GameData data, Values v) {
            IsInMenu = data.NewData.AirTemperature == 0;
            var wasInMenu = data.OldData.AirTemperature == 0;
            EnteredMenu = !wasInMenu && IsInMenu;
            ExitedMenu = wasInMenu && !IsInMenu;

            IsInPitLane = data.NewData.IsInPitLane == 1;
            IsInPitBox = data.NewData.IsInPit == 1;
            var wasInPitLane = data.OldData.IsInPitLane == 1;
            var wasInPitBox = data.OldData.IsInPit == 1;
            EnteredPitLane = !wasInPitLane && IsInPitLane;
            ExitedPitLane = wasInPitLane && !IsInPitLane;
            EnteredPitBox = !wasInPitBox && IsInPitBox;
            ExitedPitBox = wasInPitBox && !IsInPitBox;

            if (EnteredMenu) {
                RaceEngineerPlugin.LogInfo("Entered menu");
            }
            if (ExitedMenu) {
                RaceEngineerPlugin.LogInfo("Exited menu");
            }
            if (EnteredPitLane) {
                RaceEngineerPlugin.LogInfo("Entered pitlane");
            }
            if (ExitedPitLane) {
                RaceEngineerPlugin.LogInfo("Exited pitlane");
            }
            if (EnteredPitBox) {
                RaceEngineerPlugin.LogInfo("Entered pitbox");
            }
            if (ExitedPitBox) {
                RaceEngineerPlugin.LogInfo("Exited pitbox");
            }


            // In ACC AirTemp=0 if UI is visible. Nice way to identify but doesn't work in other games.
            IsOnTrack = !IsInPitLane && !data.GamePaused && (RaceEngineerPlugin.Game.IsAcc ? data.NewData.AirTemperature > 0.0 : true);
            if (RaceEngineerPlugin.Game.IsAcc && IsInMenu) {
                IsSetupMenuVisible = v.RawData.NewData.Graphics.IsSetupMenuVisible == 1;
            }

            IsMoving = data.NewData.SpeedKmh > 1;
            IsLapFinished = data.OldData.CompletedLaps < data.NewData.CompletedLaps;
            if (!_isSessionLimitSet) {
                // Need to set once as at the end of the session SessionTimeLeft == 0 and this will confuse plugin
                IsTimeLimitedSession = data.NewData.SessionTimeLeft.TotalSeconds != 0;
                IsLapLimitedSession = data.NewData.RemainingLaps != 0;
            }

            if (IsValidFuelLap && IsInPitLane) {
                RaceEngineerPlugin.LogInfo("Set 'IsValidFuelLap = false'");
                IsValidFuelLap = false;
            }
            // IsValidFuelLap &= !IsInPitLane;

            double lastLapTime = data.NewData.LastLapTime.TotalSeconds;

            if (IsLapFinished) {
                SavePrevLap = lastLapTime > 0.0
                    && IsValidFuelLap
                    && v.Car.Fuel.RemainingAtLapStart != 0.0
                    && v.Car.Fuel.RemainingAtLapStart > data.NewData.Fuel
                    && !IsInLap
                    && !IsOutLap;
                RaceEngineerPlugin.LogInfo($"'SaveLap = {SavePrevLap}', 'lastLapTime = {lastLapTime}', 'IsValidFuelLap = {IsValidFuelLap}', 'fuelUsedLapStart = {v.Car.Fuel.RemainingAtLapStart}', 'data.NewData.Fuel = {data.NewData.Fuel}'");
            }

            if (!IsInLap && (EnteredPitLane || EnteredMenu)) {
                if (IsMoving || (data.OldData.AirTemperature != 0 && data.NewData.AirTemperature == 0)) {
                    RaceEngineerPlugin.LogInfo("Set 'IsInLap = true'");
                    IsInLap = true;
                }
            }

            if (!IsOutLap && (ExitedPitLane || ExitedMenu)) {
                RaceEngineerPlugin.LogInfo("Set 'IsOutLap = true'");
                IsOutLap = true;
            }
            // IsOutLap |= ExitedPitLane;

            if (!EcuMapChangedThisLap && !IsInMenu && !IsInPitLane && data.OldData.EngineMap != data.NewData.EngineMap) {
                RaceEngineerPlugin.LogInfo("Set 'EcuMapChangedThisLap = true'");
                EcuMapChangedThisLap = true;
            }
            //EcuMapChangedThisLap |= (data.OldData.EngineMap != data.NewData.EngineMap);

            if (!RainIntensityChangedThisLap && !IsInMenu && !IsInPitLane && v.RawData.NewData.Graphics.rainIntensity != v.RawData.OldData.Graphics.rainIntensity) {
                RaceEngineerPlugin.LogInfo("Set 'RainIntensityChangedThisLap = true'");
                RainIntensityChangedThisLap = true;
            }

        }

        public void Reset(RaceSessionType sessionType = RaceSessionType.Practice) {
            IsInMenu = true;
            EnteredMenu = false;
            ExitedMenu = false;

            IsInPitLane = true;
            EnteredPitLane = false;
            ExitedPitLane = false;

            IsInPitBox = false;
            EnteredPitBox = false;
            ExitedPitBox = false;

            IsOnTrack = false;
            IsMoving = false;
            IsLapFinished = false;

            IsSetupMenuVisible = false;
            IsFuelWarning = false;
            HavePressuresChanged = false;

            HasNewStintStarted = false;
            IsValidFuelLap = false;
            IsTimeLimitedSession = false;

            IsLapLimitedSession = false;
            SavePrevLap = false;
            HasSetupChanged = false;

            IsNewEvent = true;
            IsRaceStartStintAdded = false;
            IsOutLap = !(sessionType == RaceSessionType.Hotstint || sessionType == RaceSessionType.Hotlap); // First lap of HOTSTINT/HOTLAP is proper lap.

            IsValidFuelLap = sessionType == RaceSessionType.Hotstint; // First lap of HOTSTINT is proper lap
            EcuMapChangedThisLap = false;
            RainIntensityChangedThisLap = false;

            _isSessionLimitSet = false;
        }

        public void RaceStartStintAdded() {
            IsRaceStartStintAdded = true;
        }

        public void OnNewEvent(RaceSessionType sessionType) { 
            Reset(sessionType);
            IsNewEvent = false;
        }

        public void OnSessionChange(RaceSessionType sessionType) {
            Reset(sessionType);
            IsNewEvent = false;
        }

        public void OnLapFinished(GameData data) {
            // HOTLAP doesn't have fuel usage, thus set isValidFuelLap = false in that case always, otherwise reset to valid lap in other cases
            IsValidFuelLap =  data.NewData.SessionTypeName != "HOTLAP";
            IsOutLap = false;
            IsInLap = false;
            EcuMapChangedThisLap = false;
            RaceEngineerPlugin.LogInfo($@"Set 'IsValidFuelLap = {IsValidFuelLap}', 'IsOutLap = false', 'IsInLap = false'");
        }

    }


    /// <summary>
    /// Hold current and previous boolean values 
    /// </summary>
    public class Booleans {
        public BooleansBase NewData { get; private set; }
        public BooleansBase OldData { get; private set; }

        public Booleans() { 
            NewData = new BooleansBase();
            OldData = new BooleansBase();
        }

        public void Reset(RaceSessionType sessionType = RaceSessionType.Practice) {
            RaceEngineerPlugin.LogInfo("Booleans.Reset()");
            OldData.Reset(sessionType);
            NewData.Reset(sessionType);
        }

        public void RaceStartStintAdded() {
            RaceEngineerPlugin.LogInfo("Booleans.RaceStartStintAdded()");
            NewData.RaceStartStintAdded();
        }

        public void OnNewEvent(RaceSessionType sessionType) {
            NewData.OnNewEvent(sessionType);
            OldData.OnNewEvent(sessionType);
        }

        public void OnNewSession(Values v) {
            NewData.OnSessionChange(v.Session.RaceSessionType ?? RaceSessionType.Practice);
        }

        public void OnLapFinished(GameData data) {
            NewData.OnLapFinished(data);
        }

        public void OnRegularUpdate(GameData data, Values v) {
            OldData.Update(NewData);
            NewData.Update(data, v);
        }

    }
}