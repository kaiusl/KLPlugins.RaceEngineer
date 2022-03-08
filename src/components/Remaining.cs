﻿namespace RaceEngineerPlugin.Remaining {

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

        public void OnRegularUpdate(Booleans.Booleans booleans, double timeLeft, double lapsLeft, Stats.Stats fuelUsed, Stats.Stats lapTime) {
            if (booleans.NewData.IsTimeLimitedSession) {
                time.Set(timeLeft);

                laps.Min = timeLeft / lapTime.Max;
                laps.Max = timeLeft / lapTime.Min;
                laps.Avg = timeLeft / lapTime.Avg;
            } else if (booleans.NewData.IsLapLimitedSession) {
                laps.Set(lapsLeft);
                time.Min = lapsLeft * lapTime.Min;
                time.Max = lapsLeft * lapTime.Max;
                time.Avg = lapsLeft * lapTime.Avg;
            }

            fuelNeeded.Min = laps.Min * fuelUsed.Min;
            fuelNeeded.Max = laps.Max * fuelUsed.Max;
            fuelNeeded.Avg = laps.Avg * fuelUsed.Avg;
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

        public void OnRegularUpdate(double fuelLeft, Stats.Stats fuelUsed, Stats.Stats lapTime) {
            laps.Min = fuelLeft / fuelUsed.Max;
            laps.Max = fuelLeft / fuelUsed.Min;
            laps.Avg = fuelLeft / fuelUsed.Avg;
            time.Min = laps.Min * lapTime.Min;
            time.Max = laps.Max * lapTime.Max;
            time.Avg = laps.Avg * lapTime.Avg;
        }
    }

}