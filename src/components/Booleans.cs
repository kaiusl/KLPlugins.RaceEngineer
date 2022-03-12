using GameReaderCommon;
using SimHub.Plugins;

namespace RaceEngineerPlugin.Booleans {

    /// <summary>
    /// Hold single set of boolean values
    /// </summary>
    public class BooleansBase {
        public bool IsInPitLane { get; private set; }
        public bool IsOnTrack { get; private set; }
        public bool IsMoving { get; private set; }
        public bool HasFinishedLap { get; private set; }
        public bool IsSetupMenuVisible { get; private set; }
        public bool IsFuelWarning { get; private set; }
        public bool HavePressuresChanged { get; private set; }
        public bool HasNewStintStarted { get; private set; }
        public bool IsValidFuelLap { get; private set; }
        public bool IsTimeLimitedSession { get; private set; }
        public bool IsLapLimitedSession { get; private set; }
        public bool SavePrevLap { get; private set; }
        public bool HasSetupChanged { get; private set; }
        public bool IsGameRunning { get; private set; }
        public bool IsRaceStartStintAdded { get; private set; }
        public bool IsOutLap { get; private set; }
        public bool IsInLap { get; private set; }
        public bool EnteredPitLane { get; private set; }
        public bool ExitedPitLane { get; private set; }
        public bool EcuMapChangedThisLap { get; private set; }

        private bool isSessionLimitSet = false;

        public BooleansBase() {
            Reset(null);
        }

        public void Update(BooleansBase o) {
            IsInPitLane = o.IsInPitLane;
            IsOnTrack = o.IsOnTrack;
            IsMoving = o.IsMoving;
            HasFinishedLap = o.HasFinishedLap;
            IsSetupMenuVisible = o.IsSetupMenuVisible;
            IsFuelWarning = o.IsFuelWarning;
            HavePressuresChanged = o.HavePressuresChanged;
            HasNewStintStarted = o.HasNewStintStarted;
            IsValidFuelLap = o.IsValidFuelLap;
            IsTimeLimitedSession = o.IsTimeLimitedSession;
            IsLapLimitedSession = o.IsLapLimitedSession;
            SavePrevLap = o.SavePrevLap;
            HasSetupChanged = o.HasSetupChanged;
            IsGameRunning = o.IsGameRunning;
            IsRaceStartStintAdded = o.IsRaceStartStintAdded;
            IsOutLap = o.IsOutLap;
            IsInLap = o.IsInLap;
        }

        public void Update(PluginManager pm, GameData data, double minLapTime, double fuelUsedPrevLapStart) {
            IsGameRunning = data.GameRunning;
            IsInPitLane = data.NewData.IsInPitLane == 1;
            var wasInPitLane = data.OldData.IsInPitLane == 1;
            EnteredPitLane = !wasInPitLane && IsInPitLane;
            ExitedPitLane = wasInPitLane && !IsInPitLane;
            // In ACC AirTemp=0 if UI is visible. Nice way to identify but doesn't work in other games.
            IsOnTrack = !IsInPitLane && !data.GamePaused && (RaceEngineerPlugin.GAME.IsACC ? data.NewData.AirTemperature > 0.0 : true);
            IsSetupMenuVisible = RaceEngineerPlugin.GAME.IsACC && (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.IsSetupMenuVisible") == 1;
            IsMoving = data.NewData.SpeedKmh > 1;
            HasFinishedLap = data.OldData.CompletedLaps < data.NewData.CompletedLaps;
            if (!isSessionLimitSet) {
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

            if (HasFinishedLap) {
                SavePrevLap = lastLapTime > 0.0
                    && (double.IsNaN(minLapTime) || lastLapTime < minLapTime + 30)
                    && IsValidFuelLap
                    && fuelUsedPrevLapStart != 0.0
                    && fuelUsedPrevLapStart > data.NewData.Fuel
                    && !IsInLap
                    && !IsOutLap;
                RaceEngineerPlugin.LogInfo($"'SaveLap = {SavePrevLap}', 'lastLapTime = {lastLapTime}', 'minLapTime = {minLapTime}', 'IsValidFuelLap = {IsValidFuelLap}', 'fuelUsedLapStart = {fuelUsedPrevLapStart}', 'data.NewData.Fuel = {data.NewData.Fuel}'");
            }

            if (!IsInLap && EnteredPitLane) {
                if (IsMoving || (data.OldData.AirTemperature != 0 && data.NewData.AirTemperature == 0)) {
                    RaceEngineerPlugin.LogInfo("Set 'IsInLap = true'");
                    IsInLap = true;
                }
            }

            if (!IsOutLap && ExitedPitLane) {
                RaceEngineerPlugin.LogInfo("Set 'IsOutLap = true'");
                IsOutLap = true;
            }
            // IsOutLap |= ExitedPitLane;

            if (!EcuMapChangedThisLap && !IsInPitLane && data.OldData.EngineMap != data.NewData.EngineMap) {
                RaceEngineerPlugin.LogInfo("Set 'EcuMapChangedThisLap = true'");
                EcuMapChangedThisLap = true;
            }
            //EcuMapChangedThisLap |= (data.OldData.EngineMap != data.NewData.EngineMap);

        }

        public void Reset(string sessionTypeName) {
            IsInPitLane = false;
            IsOnTrack = false;
            IsMoving = false;
            HasFinishedLap = false;
            IsSetupMenuVisible = false;
            IsFuelWarning = false;
            HavePressuresChanged = false;
            HasNewStintStarted = false;
            IsValidFuelLap = false;
            IsTimeLimitedSession = false;
            IsLapLimitedSession = false;
            SavePrevLap = false;
            HasSetupChanged = false;
            IsGameRunning = false;
            IsRaceStartStintAdded = false;
            IsOutLap = !(sessionTypeName == "7" || sessionTypeName == "HOTLAP"); // First lap of HOTSTINT/HOTLAP is proper lap.
            IsValidFuelLap = sessionTypeName == "7"; // First lap of HOTSTINT is proper lap
            EcuMapChangedThisLap = false;
            isSessionLimitSet = false;
        }

        public void RaceStartStintAdded() {
            IsRaceStartStintAdded = true;
        }

        public void OnNewEvent(string sessionTypeName) { 
            OnSessionChange(sessionTypeName);
        }

        public void OnSessionChange(string sessionTypeName) {
            IsInPitLane = false;
            IsOnTrack = false;
            IsMoving = false;
            HasFinishedLap = false;
            IsSetupMenuVisible = false;
            IsFuelWarning = false;
            SavePrevLap = false;
            HasSetupChanged = false;
            IsGameRunning = true;
            IsRaceStartStintAdded = false;
            IsOutLap = sessionTypeName != "7"; // First lap of HOTSTINT is proper lap.
            IsInLap = false;
            HavePressuresChanged = false;
            HasNewStintStarted = false;
            IsValidFuelLap = sessionTypeName == "7"; // First lap of HOTSTINT is proper lap
            isSessionLimitSet = false;
        }

        public void OnLapFinished(GameData data) {
            // HOTLAP doesn't have fuel usage, thus set isValidFuelLap = false in that case always, otherwise reset to valid lap in other cases
            IsValidFuelLap = data.NewData.SessionTypeName != "HOTLAP";
            IsOutLap = false;
            IsInLap = false;
            RaceEngineerPlugin.LogInfo($@"Set 'IsValidFuelLap = {IsValidFuelLap}', 'IsOutLap = false', 'IsInLap = false'");
        }

        public void OnGameNotRunning() {
            if (IsGameRunning) { 
                IsGameRunning = false;
            }
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

        public void Reset(string sessionTypeName) {
            RaceEngineerPlugin.LogInfo("Booleans.Reset()");
            OldData.Reset(null);
            NewData.Reset(sessionTypeName);
        }

        public void RaceStartStintAdded() {
            RaceEngineerPlugin.LogInfo("Booleans.RaceStartStintAdded()");
            NewData.RaceStartStintAdded();
        }

        public void OnGameNotRunning() {
            NewData.OnGameNotRunning();
        }

        public void OnNewEvent(string sessionTypeName) {
            NewData.OnNewEvent(sessionTypeName);
            OldData.OnNewEvent(sessionTypeName);
        }

        public void OnNewSession(string sessionTypeName) {
            NewData.OnSessionChange(sessionTypeName);
        }

        public void OnLapFinished(GameData data) {
            NewData.OnLapFinished(data);
        }

        public void OnRegularUpdate(PluginManager pm, GameData data, double minLapTime, double fuelUsedLapStart) {
            OldData.Update(NewData);
            NewData.Update(pm, data, minLapTime, fuelUsedLapStart);
        }

    }
}