using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KLPlugins.RaceEngineer.Car {
    public class CarSetup {
        public String carName { get; set; }
        public BasicSetup basicSetup { get; set; }
        public AdvancedSetup advancedSetup { get; set; }
        public int trackBopType { get; set; }
    }

    public class BasicSetup {
        public TyreSetup tyres { get; set; }
        public Alignment alignment { get; set; }
        public Electronics electronics { get; set; }
        public Strategy strategy { get; set; }
    }

    public class TyreSetup {
        public int tyreCompound { get; set; }
        public int[] tyrePressure { get; set; }
    }

    public class Alignment {
        public int[] camber { get; set; }
        public int[] toe { get; set; }
        public double[] staticCamber { get; set; }
        public double[] toeOutLinear { get; set; }
        public int casterLF { get; set; }
        public int casterRF { get; set; }
        public int steerRatio { get; set; }
    }

    public class Electronics {
        public int tC1 { get; set; }
        public int tC2 { get; set; }
        public int abs { get; set; }
        public int eCUMap { get; set; }
        public int fuelMix { get; set; }
        public int telemetryLaps { get; set; }
    }

    public class Strategy {
        public int fuel { get; set; }
        public int nPitStops { get; set; }
        public int tyreSet { get; set; }
        public int fronBrakePadCompound { get; set; }
        public int rearBrakePadCompound { get; set; }
        public PitStrategy[] pitStrategy { get; set; }
        public float fuelPerLap { get; set; }
    }

    public class PitStrategy {
        public int fuelToAdd { get; set; }
        public TyreSetup tyres { get; set; }
        public int tyreSet { get; set; }
        public int frontBrakePadCompound { get; set; }
        public int rearBrakePadCompound { get; set; }
    }

    public class AdvancedSetup {
        public MechanicalBalance mechanicalBalance { get; set; }
        public Dampers dampers { get; set; }
        public AeroBalance aeroBalance { get; set; }
        public Drivetrain driveTrain { get; set; }
    }

    public class MechanicalBalance {
        public int aRBFront { get; set; }
        public int aRBRear { get; set; }
        public int[] wheelRate { get; set; }
        public int[] bumpStopRateUp { get; set; }
        public int[] bumpStopRateDn { get; set; }
        public int[] bumpStopWindow { get; set; }
        public int brakeTorque { get; set; }
        public int brakeBias { get; set; }
    }

    public class Dampers {
        public int[] bumpSlow { get; set; }
        public int[] bumpFast { get; set; }
        public int[] reboundSlow { get; set; }
        public int[] reboundFase { get; set; }
    }

    public class AeroBalance {
        public int[] rideHeight { get; set; }
        public double[] rodLength { get; set; }
        public int splitter { get; set; }
        public int rearWing { get; set; }
        public int[] brakeDuct { get; set; }
    }

    public class Drivetrain {
        public int preload { get; set; }
    }


}