using GameReaderCommon;

using KLPlugins.RaceEngineer.Stats;

namespace KLPlugins.RaceEngineer.Remaining {

    /// <summary>
    /// Class to store and calculate current laps/time left in session and fuel needed.
    /// </summary>
    public class RemainingInSession {
        public ReadonlyMinMaxAvgView<double> Time => this._time.AsReadonlyView();
        public ReadonlyMinMaxAvgView<double> Laps => this._laps.AsReadonlyView();
        public ReadonlyMinMaxAvgView<double> FuelNeeded => this._fuelNeeded.AsReadonlyView();

        private readonly MinMaxAvg<double> _time = new(0, 0, 0);
        private readonly MinMaxAvg<double> _laps = new(0, 0, 0);
        private readonly MinMaxAvg<double> _fuelNeeded = new(0, 0, 0);

        internal void Reset() {
            this._time.Set(0);
            this._laps.Set(0);
            this._fuelNeeded.Set(0);
        }

        internal void OnRegularUpdate(GameData data, Values v) {
            if (v.Booleans.NewData.IsTimeLimitedSession) {
                var timeLeft = data.NewData.SessionTimeLeft.TotalSeconds;
                this._time.Set(timeLeft);

                var prevTimes = v.Laps.PrevTimes;
                this._laps.Min = timeLeft / prevTimes.Max;
                this._laps.Max = timeLeft / prevTimes.Min;
                this._laps.Avg = timeLeft / prevTimes.Avg;
            } else if (v.Booleans.NewData.IsLapLimitedSession) {
                var lapsLeft = data.NewData.RemainingLaps - data.NewData.TrackPositionPercent;
                this._laps.Set(lapsLeft);
                var prevTimes = v.Laps.PrevTimes;
                this._time.Min = lapsLeft * prevTimes.Min;
                this._time.Max = lapsLeft * prevTimes.Max;
                this._time.Avg = lapsLeft * prevTimes.Avg;
            }

            var prevUsedFuel = v.Car.Fuel.PrevUsedPerLap;
            this._fuelNeeded.Min = this._laps.Min * prevUsedFuel.Min;
            this._fuelNeeded.Max = this._laps.Max * prevUsedFuel.Max;
            this._fuelNeeded.Avg = this._laps.Avg * prevUsedFuel.Avg;
        }
    }

    /// <summary>
    /// Class to store and calculate time/laps left on current amount of fuel.
    /// </summary>
    public class RemainingOnFuel {
        public ReadonlyMinMaxAvgView<double> Time => this._time.AsReadonlyView();
        public ReadonlyMinMaxAvgView<double> Laps => this._laps.AsReadonlyView();

        private readonly MinMaxAvg<double> _time = new(0, 0, 0);
        private readonly MinMaxAvg<double> _laps = new(0, 0, 0);

        internal void Reset() {
            this._time.Set(0);
            this._laps.Set(0);
        }

        internal void OnRegularUpdate(Values v) {
            var prevTimes = v.Laps.PrevTimes;
            var prevUsedFuel = v.Car.Fuel.PrevUsedPerLap;
            this._laps.Min = v.Car.Fuel.Remaining / prevUsedFuel.Max;
            this._laps.Max = v.Car.Fuel.Remaining / prevUsedFuel.Min;
            this._laps.Avg = v.Car.Fuel.Remaining / prevUsedFuel.Avg;
            this._time.Min = this._laps.Min * prevTimes.Min;
            this._time.Max = this._laps.Max * prevTimes.Max;
            this._time.Avg = this._laps.Avg * prevTimes.Avg;
        }
    }

}