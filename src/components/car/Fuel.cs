using GameReaderCommon;
using SimHub.Plugins;
using System;
using RaceEngineerPlugin.Deque;

namespace RaceEngineerPlugin.Car {

    public class Fuel {
        private const string TAG = RaceEngineerPlugin.PLUGIN_NAME + " (Car.Fuel): ";

        public double Remaining { get; private set; }
        public double RemainingAtLapStart { get; private set; }
        public double LastUsedPerLap { get; private set; }
        public FixedSizeDequeStats PrevUsedPerLap { get; private set; }

        public Fuel() { 
            PrevUsedPerLap = new FixedSizeDequeStats(RaceEngineerPlugin.SETTINGS.NumPreviousValuesStored);
        }

        public void Reset() {
            LogInfo("Fuel.Reset()");
            Remaining = 0.0;
            RemainingAtLapStart = 0.0;
            LastUsedPerLap = 0.0;
            PrevUsedPerLap.Clear();
        }


        public void OnSessionChange(PluginManager pm, string carName, string trackName, Database.Database db) {
            Reset();
            int trackGrip = RaceEngineerPlugin.GAME.IsACC ? (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.trackGripStatus") : -1;

            foreach (Database.PrevData pd in db.GetPrevSessionData(carName, trackName, RaceEngineerPlugin.SETTINGS.NumPreviousValuesStored, trackGrip)) {
                LogInfo($"Read fuel '{pd.fuelUsed}' from database.");
                PrevUsedPerLap.AddToFront(pd.fuelUsed);
            }
        }

        public void OnRegularUpdate(PluginManager pm, GameData data, Booleans.Booleans booleans) {
            /////////////
            // Fuel left
            Remaining = data.NewData.Fuel;
            // Above == 0 in pits in ACC. But there is another way to calculate it.
            if (Remaining == 0.0 && RaceEngineerPlugin.GAME.IsACC) {
                double avgFuelPerLapACC = (float)pm.GetPropertyValue("GameRawData.Graphics.FuelXLap");
                double estLaps = (float)pm.GetPropertyValue("GameRawData.Graphics.fuelEstimatedLaps");
                Remaining = estLaps * avgFuelPerLapACC;
            }

            // This happens when we jump to pits, reset fuel
            if (data.OldData.Fuel != 0.0 && data.NewData.Fuel == 0.0) {
                RemainingAtLapStart = 0.0;
                LogInfo($"Reset fuel at lap start to '{RemainingAtLapStart}'");
            }

            if (booleans.NewData.IsMoving && RemainingAtLapStart == 0.0) {
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
                    LogInfo($"Set fuel at lap start to '{RemainingAtLapStart}'");
                }
            }

            if (data.NewData.IsInPitLane == 1 && data.OldData.Fuel != 0 && data.NewData.Fuel != 0 && Math.Abs(data.OldData.Fuel - data.NewData.Fuel) > 0.5) {
                RemainingAtLapStart += Remaining - data.OldData.Fuel;
                LogInfo($"Added {Remaining - data.OldData.Fuel} liters of fuel.");
                LogInfo($"Set fuel at lap start to '{RemainingAtLapStart}'");
            }
        }

        public void OnLapFinished(GameData data, Booleans.Booleans booleans) {
            LastUsedPerLap = (double)RemainingAtLapStart - data.NewData.Fuel;
            RemainingAtLapStart = data.NewData.Fuel;
            LogInfo($"Set fuel at lap start to '{RemainingAtLapStart}'");

            if (booleans.NewData.HasFinishedLap && booleans.NewData.SavePrevLap && booleans.OldData.IsValidFuelLap && LastUsedPerLap > 0) {
                LogInfo($"Stored fuel used '{LastUsedPerLap}' to deque.");
                PrevUsedPerLap.AddToFront(LastUsedPerLap);
            }
        }

        private void LogInfo(string msq) {
            if (RaceEngineerPlugin.SETTINGS.Log) {
                SimHub.Logging.Current.Info(TAG + msq);
            }
        }



    }

}