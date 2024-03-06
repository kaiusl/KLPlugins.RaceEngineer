using GameReaderCommon;

using KLPlugins.RaceEngineer.Stats;

namespace KLPlugins.RaceEngineer.Remaining {

    /// <summary>
    /// Class to store and calculate current laps/time left in session and fuel needed.
    /// </summary>
    public class RemainingInSession {
        public ReadonlyStatsView Time => this._time.AsReadonlyView();
        public ReadonlyStatsView Laps => this._laps.AsReadonlyView();
        public ReadonlyStatsView FuelNeeded => this._fuelNeeded.AsReadonlyView();

        private readonly Stats.Stats _time = new();
        private readonly Stats.Stats _laps = new();
        private readonly Stats.Stats _fuelNeeded = new();

        internal void Reset() {
            this._time.Reset();
            this._laps.Reset();
            this._fuelNeeded.Reset();
        }

        internal void OnRegularUpdate(GameData data, Values v) {
            if (v.Booleans.NewData.IsTimeLimitedSession) {
                var timeLeft = data.NewData.SessionTimeLeft.TotalSeconds;
                this._time.Set(timeLeft);

                this._laps.Min = timeLeft / v.Laps.PrevTimes.Max;
                this._laps.Max = timeLeft / v.Laps.PrevTimes.Min;
                this._laps.Avg = timeLeft / v.Laps.PrevTimes.Avg;
            } else if (v.Booleans.NewData.IsLapLimitedSession) {
                var lapsLeft = data.NewData.RemainingLaps - data.NewData.TrackPositionPercent;
                this._laps.Set(lapsLeft);
                this._time.Min = lapsLeft * v.Laps.PrevTimes.Min;
                this._time.Max = lapsLeft * v.Laps.PrevTimes.Max;
                this._time.Avg = lapsLeft * v.Laps.PrevTimes.Avg;
            }

            this._fuelNeeded.Min = this._laps.Min * v.Car.Fuel.PrevUsedPerLap.Min;
            this._fuelNeeded.Max = this._laps.Max * v.Car.Fuel.PrevUsedPerLap.Max;
            this._fuelNeeded.Avg = this._laps.Avg * v.Car.Fuel.PrevUsedPerLap.Avg;
        }
    }

    /// <summary>
    /// Class to store and calculate time/laps left on current amount of fuel.
    /// </summary>
    public class RemainingOnFuel {
        public ReadonlyStatsView Time => this._time.AsReadonlyView();
        public ReadonlyStatsView Laps => this._laps.AsReadonlyView();

        private readonly Stats.Stats _time = new();
        private readonly Stats.Stats _laps = new();

        internal void Reset() {
            this._time.Reset();
            this._laps.Reset();
        }

        internal void OnRegularUpdate(Values v) {
            this._laps.Min = v.Car.Fuel.Remaining / v.Car.Fuel.PrevUsedPerLap.Max;
            this._laps.Max = v.Car.Fuel.Remaining / v.Car.Fuel.PrevUsedPerLap.Min;
            this._laps.Avg = v.Car.Fuel.Remaining / v.Car.Fuel.PrevUsedPerLap.Avg;
            this._time.Min = this._laps.Min * v.Laps.PrevTimes.Min;
            this._time.Max = this._laps.Max * v.Laps.PrevTimes.Max;
            this._time.Avg = this._laps.Avg * v.Laps.PrevTimes.Avg;
        }
    }

}