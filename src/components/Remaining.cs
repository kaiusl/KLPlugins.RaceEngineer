namespace RaceEngineerPlugin.Remaining {

    /// <summary>
    /// Class to store and calculate current laps/time left in session and fuel needed.
    /// </summary>
    public class RemainingInSession {
        public Stats.Stats time = new Stats.Stats();
        public Stats.Stats laps= new Stats.Stats();
        public Stats.Stats fuelNeeded = new Stats.Stats();

        public void Reset() { 
            time.Reset(); 
            laps.Reset();
            fuelNeeded.Reset();
        }

        public void OnRegularUpdate(Values v, double timeLeft, double lapsLeft) {
            if (v.booleans.NewData.IsTimeLimitedSession) {
                time.Set(timeLeft);

                laps.Min = timeLeft / v.laps.PrevTimes.Max;
                laps.Max = timeLeft / v.laps.PrevTimes.Min;
                laps.Avg = timeLeft / v.laps.PrevTimes.Avg;
            } else if (v.booleans.NewData.IsLapLimitedSession) {
                laps.Set(lapsLeft);
                time.Min = lapsLeft * v.laps.PrevTimes.Min;
                time.Max = lapsLeft * v.laps.PrevTimes.Max;
                time.Avg = lapsLeft * v.laps.PrevTimes.Avg;
            }

            fuelNeeded.Min = laps.Min * v.car.Fuel.PrevUsedPerLap.Min;
            fuelNeeded.Max = laps.Max * v.car.Fuel.PrevUsedPerLap.Max;
            fuelNeeded.Avg = laps.Avg * v.car.Fuel.PrevUsedPerLap.Avg;
        }
    }

    /// <summary>
    /// Class to store and calculate time/laps left on current amount of fuel.
    /// </summary>
    public class RemainingOnFuel {
        public Stats.Stats time = new Stats.Stats();
        public Stats.Stats laps = new Stats.Stats();

        public void Reset() {
            time.Reset();
            laps.Reset();
        }

        public void OnRegularUpdate(Values v) {
            laps.Min = v.car.Fuel.Remaining / v.car.Fuel.PrevUsedPerLap.Max;
            laps.Max = v.car.Fuel.Remaining / v.car.Fuel.PrevUsedPerLap.Min;
            laps.Avg = v.car.Fuel.Remaining / v.car.Fuel.PrevUsedPerLap.Avg;
            time.Min = laps.Min * v.laps.PrevTimes.Min;
            time.Max = laps.Max * v.laps.PrevTimes.Max;
            time.Avg = laps.Avg * v.laps.PrevTimes.Avg;
        }
    }

}