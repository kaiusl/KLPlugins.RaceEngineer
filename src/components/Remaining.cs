using GameReaderCommon;

namespace RaceEngineerPlugin.Remaining {

    /// <summary>
    /// Class to store and calculate current laps/time left in session and fuel needed.
    /// </summary>
    public class RemainingInSession {
        public Stats.Stats Time = new Stats.Stats();
        public Stats.Stats Laps = new Stats.Stats();
        public Stats.Stats FuelNeeded = new Stats.Stats();

        public void Reset() { 
            Time.Reset(); 
            Laps.Reset();
            FuelNeeded.Reset();
        }

        public void OnRegularUpdate(GameData data, Values v) {
            if (v.Booleans.NewData.IsTimeLimitedSession) {
                var timeLeft = data.NewData.SessionTimeLeft.TotalSeconds;
                Time.Set(timeLeft);

                Laps.Min = timeLeft / v.Laps.PrevTimes.Max;
                Laps.Max = timeLeft / v.Laps.PrevTimes.Min;
                Laps.Avg = timeLeft / v.Laps.PrevTimes.Avg;
            } else if (v.Booleans.NewData.IsLapLimitedSession) {
                var lapsLeft = data.NewData.RemainingLaps;
                Laps.Set(lapsLeft);
                Time.Min = lapsLeft * v.Laps.PrevTimes.Min;
                Time.Max = lapsLeft * v.Laps.PrevTimes.Max;
                Time.Avg = lapsLeft * v.Laps.PrevTimes.Avg;
            }

            FuelNeeded.Min = Laps.Min * v.Car.Fuel.PrevUsedPerLap.Min;
            FuelNeeded.Max = Laps.Max * v.Car.Fuel.PrevUsedPerLap.Max;
            FuelNeeded.Avg = Laps.Avg * v.Car.Fuel.PrevUsedPerLap.Avg;
        }
    }

    /// <summary>
    /// Class to store and calculate time/laps left on current amount of fuel.
    /// </summary>
    public class RemainingOnFuel {
        public Stats.Stats Time = new Stats.Stats();
        public Stats.Stats Laps = new Stats.Stats();

        public void Reset() {
            Time.Reset();
            Laps.Reset();
        }

        public void OnRegularUpdate(Values v) {
            Laps.Min = v.Car.Fuel.Remaining / v.Car.Fuel.PrevUsedPerLap.Max;
            Laps.Max = v.Car.Fuel.Remaining / v.Car.Fuel.PrevUsedPerLap.Min;
            Laps.Avg = v.Car.Fuel.Remaining / v.Car.Fuel.PrevUsedPerLap.Avg;
            Time.Min = Laps.Min * v.Laps.PrevTimes.Min;
            Time.Max = Laps.Max * v.Laps.PrevTimes.Max;
            Time.Avg = Laps.Avg * v.Laps.PrevTimes.Avg;
        }
    }

}