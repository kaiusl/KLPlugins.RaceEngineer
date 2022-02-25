using GameReaderCommon;
using SimHub.Plugins;

namespace RaceEngineerPlugin.Booleans {
    public class Bools {
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
        public bool SaveLap { get; private set; }
        public bool HasSetupChanged { get; private set; }
        public bool IsGameRunning { get; private set; }
        public bool IsRaceStartStintAdded { get; private set; }
        public bool IsOutLap { get; private set; }
        public bool IsInLap { get; private set; }

        public Bools() {
            Reset(null);
        }

        public void Update(Bools o) {
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
            SaveLap = o.SaveLap;
            HasSetupChanged = o.HasSetupChanged;
            IsGameRunning = o.IsGameRunning;
            IsRaceStartStintAdded = o.IsRaceStartStintAdded;
            IsOutLap = o.IsOutLap;
            IsInLap = o.IsInLap;
        }

        public void Update(PluginManager pm, GameData data, double minLapTime, double fuelUsedLapStart) {
            IsInPitLane = data.NewData.IsInPitLane == 1;
            // In ACC AirTemp=0 if UI is visible. Nice way to identify but doesn't work in other games.
            IsOnTrack = !IsInPitLane && !data.GamePaused && (RaceEngineerPlugin.GAME.IsACC ? data.NewData.AirTemperature > 0.0 : true);
            if (RaceEngineerPlugin.GAME.IsACC) {
                IsSetupMenuVisible = (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.IsSetupMenuVisible") == 1;
            }
            IsMoving = data.NewData.SpeedKmh > 1;
            HasFinishedLap = data.OldData.CompletedLaps != data.NewData.CompletedLaps;
            IsTimeLimitedSession = data.NewData.SessionTimeLeft.TotalSeconds != 0;
            IsLapLimitedSession = data.NewData.RemainingLaps != 0;
            if (IsValidFuelLap && IsInPitLane) {
                IsValidFuelLap = false;
            }
            double lastLapTime = data.NewData.LastLapTime.TotalSeconds;

            SaveLap = lastLapTime > 0.0
                && (double.IsNaN(minLapTime) || lastLapTime < minLapTime + 30)
                && IsValidFuelLap
                && fuelUsedLapStart != 0.0
                && fuelUsedLapStart > data.NewData.Fuel;

            IsGameRunning = data.GameRunning;

            if (!IsInLap && data.OldData.IsInPitLane == 0 && data.NewData.IsInPitLane == 1) {
                if (IsMoving || (data.OldData.AirTemperature != 0 && data.NewData.AirTemperature == 0)) {
                    IsInLap = true;
                }
            }

            if (!IsOutLap && data.OldData.IsInPitLane == 1 && data.NewData.IsInPitLane == 0) {
                IsOutLap = true;
            }

            if (HasFinishedLap) {
                // HOTLAP doesn't have fuel usage, thus set isValidFuelLap = false in that case always, otherwise reset to valid lap in other cases
                IsValidFuelLap = data.NewData.SessionTypeName != "HOTLAP";
                IsOutLap = false;
                IsInLap = false;
            }
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
            SaveLap = false;
            HasSetupChanged = false;
            IsGameRunning = false;
            IsRaceStartStintAdded = false;
            IsOutLap = !(sessionTypeName == "7" || sessionTypeName == "HOTLAP"); // First lap of HOTSTINT/HOTLAP is proper lap.
            IsValidFuelLap = sessionTypeName == "7"; // First lap of HOTSTINT is proper lap
        }

        public void RaceStartStintAdded() {
            IsRaceStartStintAdded |= true;
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
            SaveLap = false;
            HasSetupChanged = false;
            IsGameRunning = true;
            IsRaceStartStintAdded = false;
            IsOutLap = sessionTypeName != "7"; // First lap of HOTSTINT is proper lap.
            IsInLap = false;
            HavePressuresChanged = false;
            HasNewStintStarted = false;
            IsValidFuelLap = sessionTypeName == "7"; // First lap of HOTSTINT is proper lap
        }

        public void OnGameNotRunning() {
            if (IsGameRunning) { 
                IsGameRunning = false;
            }
        }
    }



    public class Booleans { 
        public Bools NewData { get; private set; }
        public Bools OldData { get; private set; }

        public Booleans() { 
            NewData = new Bools();
            OldData = new Bools();
        }

        public void OnRegularUpdate(PluginManager pm, GameData data, double minLapTime, double fuelUsedLapStart) {
            OldData.Update(NewData);
            NewData.Update(pm, data, minLapTime, fuelUsedLapStart);
        }

        public void Reset(string sessionTypeName) { 
            OldData.Reset(null);
            NewData.Reset(sessionTypeName);
        }

        public void RaceStartStintAdded() {
            NewData.RaceStartStintAdded();
        }

        public void OnNewSession(string sessionTypeName) {
            NewData.OnSessionChange(sessionTypeName);
        }

        public void OnGameNotRunning() {
            NewData.OnGameNotRunning();
        }

        public void OnNewEvent(string sessionTypeName) {
            NewData.OnNewEvent(sessionTypeName);
            OldData.OnNewEvent(sessionTypeName);
        }
       
    }
}