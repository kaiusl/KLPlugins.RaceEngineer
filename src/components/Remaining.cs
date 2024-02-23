using GameReaderCommon;

namespace KLPlugins.RaceEngineer.Remaining {

    /// <summary>
    /// Class to store and calculate current laps/time left in session and fuel needed.
    /// </summary>
    public class RemainingInSession {
        public Stats.Stats Time = new Stats.Stats();
        public Stats.Stats Laps = new Stats.Stats();
        public Stats.Stats FuelNeeded = new Stats.Stats();

        public void Reset() {
            this.Time.Reset();
            this.Laps.Reset();
            this.FuelNeeded.Reset();
        }

        public void OnRegularUpdate(GameData data, Values v) {
            if (v.Booleans.NewData.IsTimeLimitedSession) {
                var timeLeft = data.NewData.SessionTimeLeft.TotalSeconds;
                this.Time.Set(timeLeft);

                this.Laps.Min = timeLeft / v.Laps.PrevTimes.Max;
                this.Laps.Max = timeLeft / v.Laps.PrevTimes.Min;
                this.Laps.Avg = timeLeft / v.Laps.PrevTimes.Avg;
            } else if (v.Booleans.NewData.IsLapLimitedSession) {
                var lapsLeft = data.NewData.RemainingLaps;
                this.Laps.Set(lapsLeft);
                this.Time.Min = lapsLeft * v.Laps.PrevTimes.Min;
                this.Time.Max = lapsLeft * v.Laps.PrevTimes.Max;
                this.Time.Avg = lapsLeft * v.Laps.PrevTimes.Avg;
            }

            this.FuelNeeded.Min = this.Laps.Min * v.Car.Fuel.PrevUsedPerLap.Min;
            this.FuelNeeded.Max = this.Laps.Max * v.Car.Fuel.PrevUsedPerLap.Max;
            this.FuelNeeded.Avg = this.Laps.Avg * v.Car.Fuel.PrevUsedPerLap.Avg;
        }
    }

    /// <summary>
    /// Class to store and calculate time/laps left on current amount of fuel.
    /// </summary>
    public class RemainingOnFuel {
        public Stats.Stats Time = new Stats.Stats();
        public Stats.Stats Laps = new Stats.Stats();

        public void Reset() {
            this.Time.Reset();
            this.Laps.Reset();
        }

        public void OnRegularUpdate(Values v) {
            this.Laps.Min = v.Car.Fuel.Remaining / v.Car.Fuel.PrevUsedPerLap.Max;
            this.Laps.Max = v.Car.Fuel.Remaining / v.Car.Fuel.PrevUsedPerLap.Min;
            this.Laps.Avg = v.Car.Fuel.Remaining / v.Car.Fuel.PrevUsedPerLap.Avg;
            this.Time.Min = this.Laps.Min * v.Laps.PrevTimes.Min;
            this.Time.Max = this.Laps.Max * v.Laps.PrevTimes.Max;
            this.Time.Avg = this.Laps.Avg * v.Laps.PrevTimes.Avg;
        }
    }

}