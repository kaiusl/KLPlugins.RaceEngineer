using System;

using GameReaderCommon;

using KLPlugins.RaceEngineer.Deque;

using ksBroadcastingNetwork;


namespace KLPlugins.RaceEngineer.Car {

    public class Fuel {
        public double Remaining { get; private set; }
        public double RemainingAtLapStart { get; private set; }
        public double LastUsedPerLap { get; private set; }
        public ReadonlyFixedSizeDequeStatsView PrevUsedPerLap => this._prevUsedPerLap.AsReadonlyView();

        private FixedSizeDequeStats _prevUsedPerLap { get; }

        internal Fuel() {
            this._prevUsedPerLap = new FixedSizeDequeStats(RaceEngineerPlugin.Settings.NumPreviousValuesStored, RemoveOutliers.None);
            this.Reset();
        }

        internal void Reset() {
            RaceEngineerPlugin.LogInfo("Fuel.Reset()");
            this.Remaining = 0.0;
            this.RemainingAtLapStart = 0.0;
            this.LastUsedPerLap = 0.0;
            this._prevUsedPerLap.Fill(double.NaN);
        }

        #region On... METHODS

        internal void OnNewEvent(GameData data, Values v) {
            foreach (Database.PrevData pd in v.Db.GetPrevSessionData(data, v)) {
                this._prevUsedPerLap.AddToFront(pd.fuelUsed);
            }
        }

        internal void OnSessionChange(GameData data, Values v) {
            this.Reset();

            foreach (Database.PrevData pd in v.Db.GetPrevSessionData(data, v)) {
                this._prevUsedPerLap.AddToFront(pd.fuelUsed);
            }
        }

        internal void OnLapFinished(GameData data, Values v) {
            this.LastUsedPerLap = this.RemainingAtLapStart - data.NewData.Fuel;
            this.RemainingAtLapStart = data.NewData.Fuel;
            RaceEngineerPlugin.LogInfo($"Set fuel at lap start to '{this.RemainingAtLapStart}'");

            if (v.Booleans.NewData.SavePrevLap) {
                RaceEngineerPlugin.LogInfo($"Stored fuel used '{this.LastUsedPerLap}' to deque.");
                this._prevUsedPerLap.AddToFront(this.LastUsedPerLap);
            }
        }

        internal void OnRegularUpdate(GameData data, Values v) {
            /////////////
            // Fuel left
            this.Remaining = data.NewData.Fuel;
            // Above == 0 in pits in ACC. But there is another way to calculate it.
            if (RaceEngineerPlugin.Game.IsAcc && this.Remaining == 0.0) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();

                double avgFuelPerLapACC = rawDataNew.Graphics.FuelXLap;
                double estLaps = rawDataNew.Graphics.fuelEstimatedLaps;
                this.Remaining = estLaps * avgFuelPerLapACC;
            }

            // This happens when we jump to pits, reset fuel
            if (v.Booleans.NewData.EnteredMenu) {
                this.RemainingAtLapStart = 0.0;
                RaceEngineerPlugin.LogInfo($"Reset fuel at lap start to '{this.RemainingAtLapStart}'");
            }

            if (v.Booleans.NewData.IsMoving && this.RemainingAtLapStart == 0.0) {
                bool set_lap_start_fuel = false;

                var sessType = v.Session.SessionType;
                // In race/hotstint take fuel start at the line, when the session timer starts running. Otherwise when we first start moving.
                if (RaceEngineerPlugin.Game.IsAcc && (sessType == SessionType.Race || sessType == SessionType.Hotstint)) {
                    var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();

                    var sessPhase = rawDataNew.Realtime?.Phase;
                    if ((sessPhase != null && sessPhase == SessionPhase.Session && sessPhase == SessionPhase.PreSession) || (data.NewData.SessionTimeLeft != data.OldData.SessionTimeLeft)) {
                        set_lap_start_fuel = true;
                    }
                } else {
                    set_lap_start_fuel = true;
                }

                if (set_lap_start_fuel) {
                    this.RemainingAtLapStart = data.NewData.Fuel;
                    RaceEngineerPlugin.LogInfo($"Set fuel at lap start to '{this.RemainingAtLapStart}'");
                }
            }

            if (data.NewData.IsInPitLane == 1 && data.OldData.Fuel != 0 && data.NewData.Fuel != 0 && Math.Abs(data.OldData.Fuel - data.NewData.Fuel) > 0.5) {
                this.RemainingAtLapStart += this.Remaining - data.OldData.Fuel;
                RaceEngineerPlugin.LogInfo($"Added {this.Remaining - data.OldData.Fuel} liters of fuel.\n Set fuel at lap start to '{this.RemainingAtLapStart}'");
            }
        }

        #endregion


    }

}