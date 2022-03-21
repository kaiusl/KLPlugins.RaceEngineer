using GameReaderCommon;
using SimHub.Plugins;
using System;
using RaceEngineerPlugin.Deque;
using RaceEngineerPlugin.RawData;

namespace RaceEngineerPlugin.Car {

    public class Fuel {
        public double Remaining { get; private set; }
        public double RemainingAtLapStart { get; private set; }
        public double LastUsedPerLap { get; private set; }
        public FixedSizeDequeStats PrevUsedPerLap { get; private set; }

        public Fuel() { 
            PrevUsedPerLap = new FixedSizeDequeStats(RaceEngineerPlugin.SETTINGS.NumPreviousValuesStored, RemoveOutliers.None);
            PrevUsedPerLap.Fill(double.NaN);
        }

        public void Reset() {
            RaceEngineerPlugin.LogInfo("Fuel.Reset()");
            Remaining = 0.0;
            RemainingAtLapStart = 0.0;
            LastUsedPerLap = 0.0;
            PrevUsedPerLap.Fill(double.NaN);
        }

        #region On... METHODS

        public void OnNewEvent(Values v) {
            foreach (Database.PrevData pd in v.db.GetPrevSessionData(v)) {
                RaceEngineerPlugin.LogInfo($"Read fuel '{pd.fuelUsed}' from database.");
                PrevUsedPerLap.AddToFront(pd.fuelUsed);
            }
        }

        public void OnSessionChange(Values v) {
            Reset();

            foreach (Database.PrevData pd in v.db.GetPrevSessionData(v)) {
                RaceEngineerPlugin.LogInfo($"Read fuel '{pd.fuelUsed}' from database.");
                PrevUsedPerLap.AddToFront(pd.fuelUsed);
            }
        }

        public void OnLapFinished(GameData data, Values v) {
            LastUsedPerLap = (double)RemainingAtLapStart - data.NewData.Fuel;
            RemainingAtLapStart = data.NewData.Fuel;
            RaceEngineerPlugin.LogInfo($"Set fuel at lap start to '{RemainingAtLapStart}'");

            if (v.booleans.NewData.HasFinishedLap && v.booleans.NewData.SavePrevLap && v.booleans.OldData.IsValidFuelLap && LastUsedPerLap > 0) {
                RaceEngineerPlugin.LogInfo($"Stored fuel used '{LastUsedPerLap}' to deque.");
                PrevUsedPerLap.AddToFront(LastUsedPerLap);
            }
        }

        public void OnRegularUpdate(GameData data, Values v) {
            /////////////
            // Fuel left
            Remaining = data.NewData.Fuel;
            // Above == 0 in pits in ACC. But there is another way to calculate it.
            if (v.booleans.NewData.IsInMenu && v.booleans.NewData.IsSetupMenuVisible) {
                double avgFuelPerLapACC =  v.RawData.NewData.Graphics.FuelXLap;
                double estLaps = v.RawData.NewData.Graphics.fuelEstimatedLaps;
                Remaining = estLaps * avgFuelPerLapACC;
            }

            // This happens when we jump to pits, reset fuel
            if (data.OldData.Fuel != 0.0 && data.NewData.Fuel == 0.0) {
                RemainingAtLapStart = 0.0;
                RaceEngineerPlugin.LogInfo($"Reset fuel at lap start to '{RemainingAtLapStart}'");
            }

            if (v.booleans.NewData.IsMoving && RemainingAtLapStart == 0.0) {
                bool set_lap_start_fuel = false;

                // In race/hotstint take fuel start at the line, when the session timer starts running. Otherwise when we first start moving.
                if (data.NewData.SessionTypeName == "RACE" || data.NewData.SessionTypeName == "7") { // "7" is Simhubs value for HOTSTINT
                    if (data.OldData.SessionTimeLeft != data.NewData.SessionTimeLeft) {
                        set_lap_start_fuel = true;
                    }
                } else {
                    set_lap_start_fuel = true;
                }

                if (set_lap_start_fuel) {
                    RemainingAtLapStart = data.NewData.Fuel;
                    RaceEngineerPlugin.LogInfo($"Set fuel at lap start to '{RemainingAtLapStart}'");
                }
            }

            if (data.NewData.IsInPitLane == 1 && data.OldData.Fuel != 0 && data.NewData.Fuel != 0 && Math.Abs(data.OldData.Fuel - data.NewData.Fuel) > 0.5) {
                RemainingAtLapStart += Remaining - data.OldData.Fuel;
                RaceEngineerPlugin.LogInfo($"Added {Remaining - data.OldData.Fuel} liters of fuel.\n Set fuel at lap start to '{RemainingAtLapStart}'");
            }
        }

        #endregion


    }

}