using System.Collections.Immutable;

using Newtonsoft.Json;

namespace KLPlugins.RaceEngineer.Car {
#pragma warning disable IDE1006 // Naming Styles

    public class CarSetup {

        public string carName { get; }
        public BasicSetup basicSetup { get; }
        public AdvancedSetup advancedSetup { get; }
        public int trackBopType { get; }

        [JsonConstructor]
        internal CarSetup(string carName, BasicSetup basicSetup, AdvancedSetup advancedSetup, int trackBopType) {
            this.carName = carName;
            this.basicSetup = basicSetup;
            this.advancedSetup = advancedSetup;
            this.trackBopType = trackBopType;
        }
    }

    public class BasicSetup {
        public TyreSetup tyres { get; }
        public Alignment alignment { get; }
        public Electronics electronics { get; }
        public Strategy strategy { get; }

        [JsonConstructor]
        internal BasicSetup(TyreSetup tyres, Alignment alignment, Electronics electronics, Strategy strategy) {
            this.tyres = tyres;
            this.alignment = alignment;
            this.electronics = electronics;
            this.strategy = strategy;
        }
    }

    public class TyreSetup {
        public int tyreCompound { get; }
        public ImmutableArray<int> tyrePressure { get; }

        [JsonConstructor]
        internal TyreSetup(int tyreCompound, ImmutableArray<int> tyrePressure) {
            this.tyreCompound = tyreCompound;
            this.tyrePressure = tyrePressure;
        }
    }

    public class Alignment {
        public ImmutableArray<int> camber { get; }
        public ImmutableArray<int> toe { get; }
        public ImmutableArray<double> staticCamber { get; }
        public ImmutableArray<double> toeOutLinear { get; }
        public int casterLF { get; }
        public int casterRF { get; }
        public int steerRatio { get; }

        [JsonConstructor]
        internal Alignment(ImmutableArray<int> camber, ImmutableArray<int> toe, ImmutableArray<double> staticCamber, ImmutableArray<double> toeOutLinear, int casterLF, int casterRF, int steerRatio) {
            this.camber = camber;
            this.toe = toe;
            this.staticCamber = staticCamber;
            this.toeOutLinear = toeOutLinear;
            this.casterLF = casterLF;
            this.casterRF = casterRF;
            this.steerRatio = steerRatio;
        }
    }

    public class Electronics {
        public int tC1 { get; }
        public int tC2 { get; }
        public int abs { get; }
        public int eCUMap { get; }
        public int fuelMix { get; }
        public int telemetryLaps { get; }

        [JsonConstructor]
        internal Electronics(int tC1, int tC2, int abs, int eCUMap, int fuelMix, int telemetryLaps) {
            this.tC1 = tC1;
            this.tC2 = tC2;
            this.abs = abs;
            this.eCUMap = eCUMap;
            this.fuelMix = fuelMix;
            this.telemetryLaps = telemetryLaps;
        }
    }

    public class Strategy {
        public int fuel { get; }
        public int nPitStops { get; }
        public int tyreSet { get; }
        public int frontBrakePadCompound { get; }
        public int rearBrakePadCompound { get; }
        public ImmutableArray<PitStrategy> pitStrategy { get; }
        public float fuelPerLap { get; }

        [JsonConstructor]
        internal Strategy(int fuel, int nPitStops, int tyreSet, int frontBrakePadCompound, int rearBrakePadCompound, ImmutableArray<PitStrategy> pitStrategy, float fuelPerLap) {
            this.fuel = fuel;
            this.nPitStops = nPitStops;
            this.tyreSet = tyreSet;
            this.frontBrakePadCompound = frontBrakePadCompound;
            this.rearBrakePadCompound = rearBrakePadCompound;
            this.pitStrategy = pitStrategy;
            this.fuelPerLap = fuelPerLap;
        }
    }

    public class PitStrategy {
        public int fuelToAdd { get; }
        public TyreSetup tyres { get; }
        public int tyreSet { get; }
        public int frontBrakePadCompound { get; }
        public int rearBrakePadCompound { get; }

        [JsonConstructor]
        internal PitStrategy(int fuelToAdd, TyreSetup tyres, int frontBrakePadCompound, int rearBrakePadCompound) {
            this.fuelToAdd = fuelToAdd;
            this.tyres = tyres;
            this.tyreSet = tyreSet;
            this.frontBrakePadCompound = frontBrakePadCompound;
            this.rearBrakePadCompound = rearBrakePadCompound;
        }
    }

    public class AdvancedSetup {
        public MechanicalBalance mechanicalBalance { get; }
        public Dampers dampers { get; }
        public AeroBalance aeroBalance { get; }
        public Drivetrain driveTrain { get; }

        [JsonConstructor]
        internal AdvancedSetup(MechanicalBalance mechanicalBalance, Dampers dampers, AeroBalance aeroBalance, Drivetrain driveTrain) {
            this.mechanicalBalance = mechanicalBalance;
            this.dampers = dampers;
            this.aeroBalance = aeroBalance;
            this.driveTrain = driveTrain;
        }
    }

    public class MechanicalBalance {
        public int aRBFront { get; }
        public int aRBRear { get; }
        public ImmutableArray<int> wheelRate { get; }
        public ImmutableArray<int> bumpStopRateUp { get; }
        public ImmutableArray<int> bumpStopRateDn { get; }
        public ImmutableArray<int> bumpStopWindow { get; }
        public int brakeTorque { get; }
        public int brakeBias { get; }

        [JsonConstructor]
        internal MechanicalBalance(int aRBFront, int aRBRear, ImmutableArray<int> wheelRate, ImmutableArray<int> bumpStopRateUp, ImmutableArray<int> bumpStopRateDn, ImmutableArray<int> bumpStopWindow, int brakeTorque, int brakeBias) {
            this.aRBFront = aRBFront;
            this.aRBRear = aRBRear;
            this.wheelRate = wheelRate;
            this.bumpStopRateUp = bumpStopRateUp;
            this.bumpStopRateDn = bumpStopRateDn;
            this.bumpStopWindow = bumpStopWindow;
            this.brakeTorque = brakeTorque;
            this.brakeBias = brakeBias;
        }
    }

    public class Dampers {
        public ImmutableArray<int> bumpSlow { get; }
        public ImmutableArray<int> bumpFast { get; }
        public ImmutableArray<int> reboundSlow { get; }
        public ImmutableArray<int> reboundFast { get; }

        [JsonConstructor]
        internal Dampers(ImmutableArray<int> bumpSlow, ImmutableArray<int> bumpFast, ImmutableArray<int> reboundSlow, ImmutableArray<int> reboundFast) {
            this.bumpSlow = bumpSlow;
            this.bumpFast = bumpFast;
            this.reboundSlow = reboundSlow;
            this.reboundFast = reboundFast;
        }
    }

    public class AeroBalance {
        public ImmutableArray<int> rideHeight { get; }
        public ImmutableArray<double> rodLength { get; }
        public int splitter { get; }
        public int rearWing { get; }
        public ImmutableArray<int> brakeDuct { get; }

        [JsonConstructor]
        internal AeroBalance(ImmutableArray<int> rideHeight, ImmutableArray<double> rodLength, int splitter, int rearWing, ImmutableArray<int> brakeDuct) {
            this.rideHeight = rideHeight;
            this.rodLength = rodLength;
            this.splitter = splitter;
            this.rearWing = rearWing;
            this.brakeDuct = brakeDuct;
        }
    }

    public class Drivetrain {
        public int preload { get; }

        [JsonConstructor]
        internal Drivetrain(int preload) {
            this.preload = preload;
        }
    }

}