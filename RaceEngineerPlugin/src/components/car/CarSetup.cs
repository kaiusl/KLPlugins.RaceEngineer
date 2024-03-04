using System.Collections.Immutable;

using Newtonsoft.Json;

namespace KLPlugins.RaceEngineer.Car {
#pragma warning disable IDE1006 // Naming Styles

    public class CarSetup {
        [JsonProperty(Required = Required.Always)]
        public string carName { get; }

        [JsonProperty(Required = Required.Always)]
        public BasicSetup basicSetup { get; }

        [JsonProperty(Required = Required.Always)]
        public AdvancedSetup advancedSetup { get; }

        [JsonProperty(Required = Required.Always)]
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
        [JsonProperty(Required = Required.Always)]
        public TyreSetup tyres { get; }
        [JsonProperty(Required = Required.Always)]
        public Alignment alignment { get; }
        [JsonProperty(Required = Required.Always)]
        public Electronics electronics { get; }
        [JsonProperty(Required = Required.Always)]
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
        [JsonProperty(Required = Required.Always)]
        public int tyreCompound { get; }

        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<int> tyrePressure { get; }

        [JsonConstructor]
        internal TyreSetup(int tyreCompound, ImmutableArray<int> tyrePressure) {
            this.tyreCompound = tyreCompound;
            this.tyrePressure = tyrePressure;
        }
    }

    public class Alignment {
        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<int> camber { get; }


        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<int> toe { get; }


        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<double> staticCamber { get; }


        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<double> toeOutLinear { get; }


        [JsonProperty(Required = Required.Always)]
        public int casterLF { get; }


        [JsonProperty(Required = Required.Always)]
        public int casterRF { get; }


        [JsonProperty(Required = Required.Always)]
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
        [JsonProperty(Required = Required.Always)]
        public int tC1 { get; }

        [JsonProperty(Required = Required.Always)]
        public int tC2 { get; }

        [JsonProperty(Required = Required.Always)]
        public int abs { get; }

        [JsonProperty(Required = Required.Always)]
        public int eCUMap { get; }

        [JsonProperty(Required = Required.Always)]
        public int fuelMix { get; }

        [JsonProperty(Required = Required.Always)]
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
        [JsonProperty(Required = Required.Always)]
        public int fuel { get; }

        [JsonProperty(Required = Required.Always)]
        public int nPitStops { get; }

        [JsonProperty(Required = Required.Always)]
        public int tyreSet { get; }

        [JsonProperty(Required = Required.Always)]
        public int frontBrakePadCompound { get; }

        [JsonProperty(Required = Required.Always)]
        public int rearBrakePadCompound { get; }

        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<PitStrategy> pitStrategy { get; }

        [JsonProperty(Required = Required.Always)]
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
        [JsonProperty(Required = Required.Always)]
        public int fuelToAdd { get; }

        [JsonProperty(Required = Required.Always)]
        public TyreSetup tyres { get; }

        [JsonProperty(Required = Required.Always)]
        public int tyreSet { get; }

        [JsonProperty(Required = Required.Always)]
        public int frontBrakePadCompound { get; }

        [JsonProperty(Required = Required.Always)]
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
        [JsonProperty(Required = Required.Always)]
        public MechanicalBalance mechanicalBalance { get; }

        [JsonProperty(Required = Required.Always)]
        public Dampers dampers { get; }

        [JsonProperty(Required = Required.Always)]
        public AeroBalance aeroBalance { get; }

        [JsonProperty(Required = Required.Always)]
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
        [JsonProperty(Required = Required.Always)]
        public int aRBFront { get; }

        [JsonProperty(Required = Required.Always)]
        public int aRBRear { get; }

        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<int> wheelRate { get; }

        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<int> bumpStopRateUp { get; }

        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<int> bumpStopRateDn { get; }

        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<int> bumpStopWindow { get; }

        [JsonProperty(Required = Required.Always)]
        public int brakeTorque { get; }

        [JsonProperty(Required = Required.Always)]
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
        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<int> bumpSlow { get; }

        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<int> bumpFast { get; }

        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<int> reboundSlow { get; }

        [JsonProperty(Required = Required.Always)]
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
        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<int> rideHeight { get; }

        [JsonProperty(Required = Required.Always)]
        public ImmutableArray<double> rodLength { get; }

        [JsonProperty(Required = Required.Always)]
        public int splitter { get; }

        [JsonProperty(Required = Required.Always)]
        public int rearWing { get; }

        [JsonProperty(Required = Required.Always)]
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
        [JsonProperty(Required = Required.Always)]
        public int preload { get; }

        [JsonConstructor]
        internal Drivetrain(int preload) {
            this.preload = preload;
        }
    }

}