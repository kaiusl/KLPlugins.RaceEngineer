using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ACSharedMemory.ACC.MMFModels;

using GameReaderCommon;

using KLPlugins.RaceEngineer.Car;

namespace KLPlugins.RaceEngineer.Database {
    internal struct Event {
        internal string? CarId;
        internal string? TrackId;
        internal string StartTime;
        internal string GameVersion;

        internal Event(GameData data, Values v) {
            this.CarId = v.Car.Name;
            this.TrackId = v.Track.Name;
            this.StartTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss");
            if (RaceEngineerPlugin.Game.IsAcc) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();

                this.GameVersion = $"{rawDataNew.StaticInfo.ACVersion}/{rawDataNew.StaticInfo.SMVersion}";
            } else {
                this.GameVersion = "unknown";
            }

        }

        public override readonly string ToString() {
            return $"CarId = {this.CarId}, TrackId = {this.TrackId}, StartTime = {this.StartTime}, GameVersion = {this.GameVersion}";
        }
    }

    internal struct Session(Values v, long eventId) {
        internal long EventId = eventId;
        internal string SessionType = v.Session.SessionType.ToString();
        internal int TimeMultiplier = v.Session.TimeMultiplier;
        internal string StartTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss");

        public override readonly string ToString() {
            return $"EventId = {this.EventId}, SessionType = {this.SessionType}, TimeMultiplier = {this.TimeMultiplier}, StartTime = {this.StartTime}";
        }
    }

    internal struct Stint {
        internal long SessionId;
        internal int StintNr;
        internal string StartTime;
        internal string? TyreCompound;
        internal WheelsData<double> TyrePresIn;
        internal int BrakePadFront;
        internal int BrakePadRear;
        internal int BrakePadNr;
        internal int BrakeDuctFront;
        internal int BrakeDuctRear;
        internal int TyreSet;
        internal int[] Camber;
        internal int[] Toe;
        internal int CasterLf;
        internal int CasterRf;

        internal Stint(GameData data, Values v, long sessionId) {
            this.SessionId = sessionId;
            string stime = DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss");

            this.StintNr = v.Laps.StintNr;
            this.StartTime = stime;
            this.TyreCompound = v.Car.Tyres.Name;

            this.TyrePresIn = v.Car.Tyres.CurrentInputPres;

            if (RaceEngineerPlugin.Game.IsAcc) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();

                this.BrakePadFront = rawDataNew.Physics.frontBrakeCompound + 1;
                this.BrakePadRear = rawDataNew.Physics.rearBrakeCompound + 1;
            } else {
                this.BrakePadFront = -1;
                this.BrakePadRear = -1;
            }

            this.TyreSet = v.Car.Tyres.CurrentTyreSet;

            this.BrakePadNr = v.Car.Brakes.SetNr;

            if (v.Car.Setup != null) {
                this.BrakeDuctFront = v.Car.Setup.advancedSetup.aeroBalance.brakeDuct[0];
                this.BrakeDuctRear = v.Car.Setup.advancedSetup.aeroBalance.brakeDuct[1];
                this.Camber = v.Car.Setup.basicSetup.alignment.camber.ToArray();
                this.Toe = v.Car.Setup.basicSetup.alignment.toe.ToArray();
                this.CasterLf = v.Car.Setup.basicSetup.alignment.casterLF;
                this.CasterRf = v.Car.Setup.basicSetup.alignment.casterRF;
            } else {
                this.BrakeDuctFront = -1;
                this.BrakeDuctRear = -1;
                this.Camber = new int[4];
                this.Toe = new int[4];

                for (var i = 0; i < 4; i++) {
                    this.Camber[i] = -1;
                    this.Toe[i] = -1;
                }

                this.CasterLf = -1;
                this.CasterRf = -1;
            }
        }

        public override readonly string ToString() {
            return $@"SessionId = {this.SessionId}; StartTime = {this.StartTime}; StintNr = {this.StintNr};
	TyreCompount = {this.TyreCompound}; TyrePresIn = {WheelsDataToString(this.TyrePresIn)}; TyreSet = {this.TyreSet};
	BrakePad = [F: {this.BrakePadFront}, R: {this.BrakePadRear}]; BrakePadNr = {this.BrakePadNr}; BrakeDuct = [F: {this.BrakeDuctFront}, R: {this.BrakeDuctRear}],
	Camber = {ArrayToString(this.Camber)}; Toe = {ArrayToString(this.Toe)}, Caster = [{this.CasterLf}, {this.CasterRf}]";
        }

        private static string ArrayToString<T>(T[] a) {
            return $"[{a[0]}, {a[1]}, {a[2]}, {a[3]}]";
        }

        private static string WheelsDataToString<T>(WheelsData<T> a) {
            return $"[{a[0]}, {a[1]}, {a[2]}, {a[3]}]";
        }
    }

    internal class Lap {
        internal long StintId;
        internal int SessionLapNr;
        internal int StintLapNr;
        internal int TyresetLapNr;
        internal int BrakesLapNr;
        internal double AirTemp;
        internal double AirTempDelta;
        internal double TrackTemp;
        internal double TrackTempDelta;
        internal double LapTime;
        internal double FuelUsed;
        internal double FuelLeft;

        internal double[] TyrePresAvg = [0.0, 0.0, 0.0, 0.0];
        internal double[] TyrePresMin = [0.0, 0.0, 0.0, 0.0];
        internal double[] TyrePresMax = [0.0, 0.0, 0.0, 0.0];
        internal double[] TyrePresLoss = [0.0, 0.0, 0.0, 0.0];
        internal bool[] TyrePresLossLap = [false, false, false, false];

        internal double[] TyreTempAvg = [0.0, 0.0, 0.0, 0.0];
        internal double[] TyreTempMin = [0.0, 0.0, 0.0, 0.0];
        internal double[] TyreTempMax = [0.0, 0.0, 0.0, 0.0];

        internal double[] BrakeTempAvg = [0.0, 0.0, 0.0, 0.0];
        internal double[] BrakeTempMin = [0.0, 0.0, 0.0, 0.0];
        internal double[] BrakeTempMax = [0.0, 0.0, 0.0, 0.0];

        internal double[] TyreLifeLeft = [0.0, 0.0, 0.0, 0.0];

        internal double[] PadLifeLeft = [0.0, 0.0, 0.0, 0.0];
        internal double[] DiscLifeLeft = [0.0, 0.0, 0.0, 0.0];

        internal int Abs;
        internal int Tc;
        internal int Tc2;
        internal int EcuMap;
        internal bool EcuMapChanged;
        internal int TrackGripStatus;
        internal bool IsValid;
        internal bool IsValidFuelLap;
        internal bool IsOutLap;
        internal bool IsInLap;
        internal int RainIntensity;
        internal bool RainIntensityChanged;

        internal Lap() { }


        internal Lap(GameData data, Values v, long stint_id) {
            this.Update(data, v, stint_id);
        }


        internal void Update(GameData data, Values v, long stint_id) {
            this.StintId = stint_id;
            this.SessionLapNr = data.NewData.CompletedLaps;
            this.StintLapNr = v.Laps.StintLaps;
            this.TyresetLapNr = v.Car.Tyres.GetCurrentSetLaps();
            this.BrakesLapNr = v.Car.Brakes.LapsNr;
            this.AirTemp = data.NewData.AirTemperature;
            this.AirTempDelta = Math.Round(data.NewData.AirTemperature - v.Weather.AirTempAtLapStart, 2);
            this.TrackTemp = data.NewData.RoadTemperature;
            this.TrackTempDelta = Math.Round(data.NewData.RoadTemperature - v.Weather.TrackTempAtLapStart, 2);
            this.LapTime = v.Laps.LastTime;
            this.FuelUsed = v.Car.Fuel.LastUsedPerLap;
            this.FuelLeft = v.Car.Fuel.Remaining;

            for (var i = 0; i < 4; i++) {
                this.TyrePresAvg[i] = v.Car.Tyres.PresOverLap[i].Avg;
                this.TyrePresMin[i] = v.Car.Tyres.PresOverLap[i].Min;
                this.TyrePresMax[i] = v.Car.Tyres.PresOverLap[i].Max;
                this.TyrePresLoss[i] = Math.Round(v.Car.Tyres.PresLoss[i], 2);
                this.TyrePresLossLap[i] = v.Car.Tyres.PresLossLap[i];

                this.TyreTempAvg[i] = v.Car.Tyres.TempOverLap[i].Avg;
                this.TyreTempMin[i] = v.Car.Tyres.TempOverLap[i].Min;
                this.TyreTempMax[i] = v.Car.Tyres.TempOverLap[i].Max;

                this.BrakeTempAvg[i] = v.Car.Brakes.TempOverLap[i].Avg;
                this.BrakeTempMin[i] = v.Car.Brakes.TempOverLap[i].Min;
                this.BrakeTempMax[i] = v.Car.Brakes.TempOverLap[i].Max;
            }

            this.Abs = data.NewData.ABSLevel;
            this.Tc = data.NewData.TCLevel;
            this.EcuMap = data.NewData.EngineMap;
            this.EcuMapChanged = v.Booleans.OldData.EcuMapChangedThisLap;

            if (RaceEngineerPlugin.Game.IsAcc) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();

                this.Tc2 = rawDataNew.Graphics.TCCut;

                for (var i = 0; i < 4; i++) {
                    this.PadLifeLeft[i] = rawDataNew.Physics.padLife[i];
                    this.DiscLifeLeft[i] = rawDataNew.Physics.discLife[i];
                }

                this.TrackGripStatus = (int)rawDataNew.Graphics.trackGripStatus;
                this.RainIntensity = (int)rawDataNew.Graphics.rainIntensity;
            } else {
                this.Tc2 = -1;
                for (var i = 0; i < 4; i++) {
                    this.PadLifeLeft[i] = -1;
                }

                this.TrackGripStatus = -1;
                this.RainIntensity = -1;
            }

            this.IsValid = v.Booleans.NewData.SavePrevLap;
            // Need to use booleans.OldData which is the last point on finished lap
            this.IsValidFuelLap = v.Booleans.OldData.IsValidFuelLap;
            this.IsOutLap = v.Booleans.OldData.IsOutLap;
            this.IsInLap = v.Booleans.OldData.IsInLap;



            this.RainIntensityChanged = v.Booleans.OldData.RainIntensityChangedThisLap;
        }

        public override string ToString() {
            return $@"StintId = {this.StintId}; SessionLapNr = {this.SessionLapNr}; StintLapNr = {this.StintLapNr}; TyresetLapNr = {this.TyresetLapNr}; BrakesLapNr = {this.BrakesLapNr};
	AirTemp = {this.AirTemp}; AirTempDelta = {this.AirTempDelta}; TrackTemp = {this.TrackTemp}; TrackTempDelta = {this.TrackTempDelta};
	LapTime = {this.LapTime}; FuelUsed = {this.FuelUsed}; FuelLeft = {this.FuelLeft};
	TyrePresAvg = {this.ArrayToString(this.TyrePresAvg)}; TyrePresMin = {this.ArrayToString(this.TyrePresMin)}; TyrePresMax = {this.ArrayToString(this.TyrePresMax)};
	TyrePresLoss = {this.ArrayToString(this.TyrePresLoss)}; TyrePressLossLap = {this.ArrayToString(this.TyrePresLossLap)};
	TyreTempAvg = {this.ArrayToString(this.TyreTempAvg)}; TyreTempMin = {this.ArrayToString(this.TyreTempMin)}; TyreTempMax = {this.ArrayToString(this.TyreTempMax)};
	BrakeTempAvg = {this.ArrayToString(this.BrakeTempAvg)}; BrakeTempMin = {this.ArrayToString(this.BrakeTempMin)}; BrakeTempMax = {this.ArrayToString(this.BrakeTempMax)};
	TyreLifeLeft = {this.ArrayToString(this.TyreLifeLeft)}; PadLifeLeft = {this.ArrayToString(this.PadLifeLeft)}; DiscLifeLeft = {this.ArrayToString(this.DiscLifeLeft)};
	Abs = {this.Abs}; Tc = {this.Tc}; Tc2 = {this.Tc2}; EcuMap = {this.EcuMap}; EcuMapChanged = {this.EcuMapChanged};
	TrackGripStatus = {this.TrackGripStatus}; RainIntensity = {this.RainIntensity}; RainIntensityChanged = {this.RainIntensityChanged};
	IsValid = {this.IsValid}; IsValidFuelLap = {this.IsValidFuelLap}; IsOutLap = {this.IsOutLap}; IsInLap = {this.IsInLap};";
        }

        private string ArrayToString<T>(T[] a) {
            return $"[{a[0]}, {a[1]}, {a[2]}, {a[3]}]";
        }

    }


    /// <summary>
    /// Handles data collection/storing for plugin.
    /// </summary>
    internal class Database {
        private SQLiteConnection? _conn;

        private readonly SQLiteCommand? _insertEventCmd;
        private readonly SQLiteCommand? _insertSessionCmd;
        private readonly SQLiteCommand? _insertStintCmd;
        private readonly SQLiteCommand? _insertLapCmd;

        private long _eventId;
        private long _sessionId;
        private long _stintId;
        private readonly Mutex _dbMutex = new();

        internal Database() {
            var location = $@"{RaceEngineerPlugin.Settings.DataLocation}\{RaceEngineerPlugin.Game.Name}\data.db";
            if (!File.Exists(location)) {
                Directory.CreateDirectory(Path.GetDirectoryName(location));
            }
            this._conn = new SQLiteConnection($"Data Source={location}");
            try {
                this._conn.Open();
                this.eventsTable.CreateTable(this._conn);
                this.sessionsTable.CreateTableWForeignKey(this._conn, $"FOREIGN KEY({EVENT_ID}) REFERENCES {this.eventsTable.name}({EVENT_ID})");
                this.stintsTable.CreateTableWForeignKey(this._conn, $"FOREIGN KEY({SESSION_ID}) REFERENCES {this.sessionsTable.name}({SESSION_ID})");
                this.lapsTable.CreateTableWForeignKey(this._conn, $"FOREIGN KEY({STINT_ID}) REFERENCES {this.stintsTable.name}({STINT_ID})");

                this._insertEventCmd = this.eventsTable.CreateInsertCmdWReturning(this._conn, EVENT_ID);
                this._insertSessionCmd = this.sessionsTable.CreateInsertCmdWReturning(this._conn, SESSION_ID);
                this._insertStintCmd = this.stintsTable.CreateInsertCmdWReturning(this._conn, STINT_ID);
                this._insertLapCmd = this.lapsTable.CreateInsertCmd(this._conn);

                RaceEngineerPlugin.LogInfo($"Opened database from '{location}'");
            } catch (Exception ex) {
                RaceEngineerPlugin.LogError($"Failed to open DB. location={location} Error msq: {ex}");
            }
        }

        #region IDisposable
        ~Database() {
            this.Dispose(false);
        }

        private bool isDisposed = false;
        protected virtual void Dispose(bool disposing) {
            if (!this.isDisposed) {
                this._dbMutex.WaitOne();

                if (disposing) {
                    this._insertEventCmd?.Dispose();
                    this._insertSessionCmd?.Dispose();
                    this._insertStintCmd?.Dispose();
                    this._insertLapCmd?.Dispose();
                }

                if (this._conn != null) {
                    this._conn.Close();
                    this._conn.Dispose();
                    this._conn = null;
                }

                RaceEngineerPlugin.LogInfo("Disposed.");
                this.isDisposed = true;
                this._dbMutex.ReleaseMutex();
                this._dbMutex.Dispose();
            }
        }

        internal void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region TABLE DEFINITIONS

        private const string EVENT_ID = "event_id";
        private const string CAR_ID = "car_id";
        private const string TRACK_ID = "track_id";
        private const string START_TIME = "start_time";
        private const string GAME_VERSION = "game_version";

        private readonly DBTable eventsTable = new("events", [
            new DBField(EVENT_ID, "INTEGER PRIMARY KEY"),
            new DBField(CAR_ID, "TEXT"),
            new DBField(TRACK_ID, "TEXT"),
            new DBField(START_TIME, "TEXT"),
            new DBField(GAME_VERSION, "TEXT")
        ]);

        private const string SESSION_ID = "session_id";
        private const string SESSION_TYPE = "session_type";
        private const string TIME_MULTIPLIER = "time_multiplier";

        private readonly DBTable sessionsTable = new("sessions", [
            new DBField(SESSION_ID, "INTEGER PRIMARY KEY"),
            new DBField(EVENT_ID, "INTEGER"),
            new DBField(SESSION_TYPE, "TEXT"),
            new DBField(START_TIME, "TEXT"),
            new DBField(TIME_MULTIPLIER, "INTEGER")
        ]);

        private static readonly string[] TYRES = ["fl", "fr", "rl", "rr"];
        private const string STINT_ID = "stint_id";
        private const string STINT_NR = "stint_nr";
        private const string TYRE_COMPOUND = "tyre_compound";
        private const string TYRE_PRES_IN = "tyre_pres_in";
        private const string BRAKE_PAD_FRONT = "brake_pad_front";
        private const string BRAKE_PAD_REAR = "brake_pad_rear";
        private const string BRAKE_PAD_NR = "brake_pad_nr";
        private const string BRAKE_DUCT_FRONT = "brake_duct_front";
        private const string BRAKE_DUCT_REAR = "brake_duct_rear";
        private const string TYRE_SET = "tyre_set";
        private const string CAMBER = "camber";
        private const string TOE = "toe";
        private const string CASTER = "caster";

        private readonly DBTable stintsTable = new("stints", [
            new DBField(STINT_ID, "INTEGER PRIMARY KEY"),
            new DBField(SESSION_ID, "INTEGER"),
            new DBField(STINT_NR, "INTEGER"),
            new DBField(START_TIME, "TEXT"),
            new DBField(TYRE_COMPOUND, "TEXT"),
            new DBField(TYRE_PRES_IN + $"_{TYRES[0]}", "REAL"),
            new DBField(TYRE_PRES_IN + $"_{TYRES[1]}", "REAL"),
            new DBField(TYRE_PRES_IN + $"_{TYRES[2]}", "REAL"),
            new DBField(TYRE_PRES_IN + $"_{TYRES[3]}", "REAL"),
            new DBField(BRAKE_PAD_FRONT, "INTEGER"),
            new DBField(BRAKE_PAD_REAR, "INTEGER"),
            new DBField(BRAKE_PAD_NR, "INTEGER"),
            new DBField(BRAKE_DUCT_FRONT, "INTEGER"),
            new DBField(BRAKE_DUCT_REAR, "INTEGER"),
            new DBField(TYRE_SET, "INTEGER"),
            new DBField(CAMBER + $"_{TYRES[0]}", "INTEGER"),
            new DBField(CAMBER + $"_{TYRES[1]}", "INTEGER"),
            new DBField(CAMBER + $"_{TYRES[2]}", "INTEGER"),
            new DBField(CAMBER + $"_{TYRES[3]}", "INTEGER"),
            new DBField(TOE + $"_{TYRES[0]}", "INTEGER"),
            new DBField(TOE + $"_{TYRES[1]}", "INTEGER"),
            new DBField(TOE + $"_{TYRES[2]}", "INTEGER"),
            new DBField(TOE + $"_{TYRES[3]}", "INTEGER"),
            new DBField(CASTER + $"_{TYRES[0]}", "INTEGER"),
            new DBField(CASTER + $"_{TYRES[1]}", "INTEGER"),
        ]);

        private const string LAP_ID = "lap_id";
        private const string SESSION_LAP_NR = "session_lap_nr";
        private const string STINT_LAP_NR = "stint_lap_nr";
        private const string TYRESET_LAP_NR = "tyreset_lap_nr";
        private const string BRAKE_PAD_LAP_NR = "brake_pad_lap_nr";
        private const string AIR_TEMP = "air_temp";
        private const string AIR_TEMP_DELTA = "air_temp_delta";
        private const string TRACK_TEMP = "track_temp";
        private const string TRACK_TEMP_DELTA = "track_temp_delta";
        private const string LAP_TIME = "lap_time";
        private const string FUEL_USED = "fuel_used";
        private const string FUEL_LEFT = "fuel_left";
        private const string TYRE_PRES_AVG = "tyre_pres_avg";
        private const string TYRE_PRES_MIN = "tyre_pres_min";
        private const string TYRE_PRES_MAX = "tyre_pres_max";
        private const string TYRE_PRES_LOSS = "tyre_pres_loss";
        private const string TYRE_PRES_LOSS_LAP = "tyre_pres_loss_lap";
        private const string TYRE_TEMP_AVG = "tyre_temp_avg";
        private const string TYRE_TEMP_MIN = "tyre_temp_min";
        private const string TYRE_TEMP_MAX = "tyre_temp_max";
        private const string BRAKE_TEMP_AVG = "brake_temp_avg";
        private const string BRAKE_TEMP_MIN = "brake_temp_min";
        private const string BRAKE_TEMP_MAX = "brake_temp_max";
        private const string TYRE_LIFE_LEFT = "tyre_life_left";
        private const string PAD_LIFE_LEFT = "pad_life_left";
        private const string DISC_LIFE_LEFT = "disc_life_left";
        private const string ABS = "abs";
        private const string TC = "tc";
        private const string TC2 = "tc2";
        private const string ECU_MAP = "ecu_map";
        private const string ECU_MAP_CHANGED = "ecu_map_changed";
        private const string TRACK_GRIP_STATUS = "track_grip_status";
        private const string IS_VALID = "is_valid";
        private const string IS_VALID_FUEL_LAP = "is_valid_fuel_lap";
        private const string IS_OUTLAP = "is_outlap";
        private const string IS_INLAP = "is_inlap";
        private const string RAIN_INTENSITY = "rain_intensity";
        private const string RAIN_INTENSITY_CHANGED = "rain_intensity_changed";

        private readonly DBTable lapsTable = new("laps", [
            new DBField(LAP_ID, "INTEGER PRIMARY KEY"),
            new DBField(STINT_ID, "INTEGER"),
            new DBField(SESSION_LAP_NR, "INTEGER"),
            new DBField(STINT_LAP_NR, "INTEGER"),
            new DBField(TYRESET_LAP_NR, "INTEGER"),
            new DBField(BRAKE_PAD_LAP_NR, "INTEGER"),
            new DBField(AIR_TEMP, "REAL"),
            new DBField(AIR_TEMP_DELTA, "REAL"),
            new DBField(TRACK_TEMP, "REAL"),
            new DBField(TRACK_TEMP_DELTA, "REAL"),
            new DBField(LAP_TIME, "REAL"),
            new DBField(FUEL_USED, "REAL"),
            new DBField(FUEL_LEFT, "REAL"),

            new DBField(TYRE_PRES_AVG + $"_{TYRES[0]}", "REAL"),
            new DBField(TYRE_PRES_AVG + $"_{TYRES[1]}", "REAL"),
            new DBField(TYRE_PRES_AVG + $"_{TYRES[2]}", "REAL"),
            new DBField(TYRE_PRES_AVG + $"_{TYRES[3]}", "REAL"),
            new DBField(TYRE_PRES_MIN + $"_{TYRES[0]}", "REAL"),
            new DBField(TYRE_PRES_MIN + $"_{TYRES[1]}", "REAL"),
            new DBField(TYRE_PRES_MIN + $"_{TYRES[2]}", "REAL"),
            new DBField(TYRE_PRES_MIN + $"_{TYRES[3]}", "REAL"),
            new DBField(TYRE_PRES_MAX + $"_{TYRES[0]}", "REAL"),
            new DBField(TYRE_PRES_MAX + $"_{TYRES[1]}", "REAL"),
            new DBField(TYRE_PRES_MAX + $"_{TYRES[2]}", "REAL"),
            new DBField(TYRE_PRES_MAX + $"_{TYRES[3]}", "REAL"),
            new DBField(TYRE_PRES_LOSS + $"_{TYRES[0]}", "REAL"),
            new DBField(TYRE_PRES_LOSS + $"_{TYRES[1]}", "REAL"),
            new DBField(TYRE_PRES_LOSS + $"_{TYRES[2]}", "REAL"),
            new DBField(TYRE_PRES_LOSS + $"_{TYRES[3]}", "REAL"),
            new DBField(TYRE_PRES_LOSS_LAP + $"_{TYRES[0]}", "INTEGER"),
            new DBField(TYRE_PRES_LOSS_LAP + $"_{TYRES[1]}", "INTEGER"),
            new DBField(TYRE_PRES_LOSS_LAP + $"_{TYRES[2]}", "INTEGER"),
            new DBField(TYRE_PRES_LOSS_LAP + $"_{TYRES[3]}", "INTEGER"),

            new DBField(TYRE_TEMP_AVG + $"_{TYRES[0]}", "REAL"),
            new DBField(TYRE_TEMP_AVG + $"_{TYRES[1]}", "REAL"),
            new DBField(TYRE_TEMP_AVG + $"_{TYRES[2]}", "REAL"),
            new DBField(TYRE_TEMP_AVG + $"_{TYRES[3]}", "REAL"),
            new DBField(TYRE_TEMP_MIN + $"_{TYRES[0]}", "REAL"),
            new DBField(TYRE_TEMP_MIN + $"_{TYRES[1]}", "REAL"),
            new DBField(TYRE_TEMP_MIN + $"_{TYRES[2]}", "REAL"),
            new DBField(TYRE_TEMP_MIN + $"_{TYRES[3]}", "REAL"),
            new DBField(TYRE_TEMP_MAX + $"_{TYRES[0]}", "REAL"),
            new DBField(TYRE_TEMP_MAX + $"_{TYRES[1]}", "REAL"),
            new DBField(TYRE_TEMP_MAX + $"_{TYRES[2]}", "REAL"),
            new DBField(TYRE_TEMP_MAX + $"_{TYRES[3]}", "REAL"),

            new DBField(BRAKE_TEMP_AVG + $"_{TYRES[0]}", "REAL"),
            new DBField(BRAKE_TEMP_AVG + $"_{TYRES[1]}", "REAL"),
            new DBField(BRAKE_TEMP_AVG + $"_{TYRES[2]}", "REAL"),
            new DBField(BRAKE_TEMP_AVG + $"_{TYRES[3]}", "REAL"),
            new DBField(BRAKE_TEMP_MIN + $"_{TYRES[0]}", "REAL"),
            new DBField(BRAKE_TEMP_MIN + $"_{TYRES[1]}", "REAL"),
            new DBField(BRAKE_TEMP_MIN + $"_{TYRES[2]}", "REAL"),
            new DBField(BRAKE_TEMP_MIN + $"_{TYRES[3]}", "REAL"),
            new DBField(BRAKE_TEMP_MAX + $"_{TYRES[0]}", "REAL"),
            new DBField(BRAKE_TEMP_MAX + $"_{TYRES[1]}", "REAL"),
            new DBField(BRAKE_TEMP_MAX + $"_{TYRES[2]}", "REAL"),
            new DBField(BRAKE_TEMP_MAX + $"_{TYRES[3]}", "REAL"),

            new DBField(TYRE_LIFE_LEFT + $"_{TYRES[0]}", "REAL"),
            new DBField(TYRE_LIFE_LEFT + $"_{TYRES[1]}", "REAL"),
            new DBField(TYRE_LIFE_LEFT + $"_{TYRES[2]}", "REAL"),
            new DBField(TYRE_LIFE_LEFT + $"_{TYRES[3]}", "REAL"),

            new DBField(PAD_LIFE_LEFT + $"_{TYRES[0]}", "REAL"),
            new DBField(PAD_LIFE_LEFT + $"_{TYRES[1]}", "REAL"),
            new DBField(PAD_LIFE_LEFT + $"_{TYRES[2]}", "REAL"),
            new DBField(PAD_LIFE_LEFT + $"_{TYRES[3]}", "REAL"),
            new DBField(DISC_LIFE_LEFT + $"_{TYRES[0]}", "REAL"),
            new DBField(DISC_LIFE_LEFT + $"_{TYRES[1]}", "REAL"),
            new DBField(DISC_LIFE_LEFT + $"_{TYRES[2]}", "REAL"),
            new DBField(DISC_LIFE_LEFT + $"_{TYRES[3]}", "REAL"),
            new DBField(ABS, "INTEGER"),
            new DBField(TC, "INTEGER"),
            new DBField(TC2, "INTEGER"),
            new DBField(ECU_MAP, "INTEGER"),
            new DBField(ECU_MAP_CHANGED, "INTEGER"),
            new DBField(TRACK_GRIP_STATUS, "INTEGER"),
            new DBField(IS_VALID, "INTEGER"),
            new DBField(IS_VALID_FUEL_LAP, "INTEGER"),
            new DBField(IS_OUTLAP, "INTEGER"),
            new DBField(IS_INLAP, "INTEGER"),
            new DBField(RAIN_INTENSITY, "INTEGER"),
            new DBField(RAIN_INTENSITY_CHANGED, "INTEGER")
        ]);

        #endregion

        #region INSERTS

        private void SetParam(SQLiteCommand cmd, string name, object? value) {
            cmd.Parameters["@" + name].Value = value?.ToString();
        }

        private void SetParam(SQLiteCommand cmd, string name, bool value) {
            this.SetParam(cmd, name, value ? 1 : 0);
        }

        internal void InsertEvent(GameData data, Values v) {
            var e = new Event(data, v);
            RaceEngineerPlugin.LogInfo(e.ToString());
            //this.InsertEvent(e);
            _ = Task.Run(() => this.InsertEvent(e));
        }

        internal void InsertEvent(Event e) {
            if (this._insertEventCmd == null) return;

            this._dbMutex.WaitOne();
            try {
                this.SetParam(this._insertEventCmd, CAR_ID, e.CarId);
                this.SetParam(this._insertEventCmd, TRACK_ID, e.TrackId);
                this.SetParam(this._insertEventCmd, START_TIME, e.StartTime);
                this.SetParam(this._insertEventCmd, GAME_VERSION, e.GameVersion);
                this._eventId = (long)this._insertEventCmd.ExecuteScalar();
            } catch (Exception ex) {
                RaceEngineerPlugin.LogError($"Failed to insert event to DB: {ex}");
            }

            this._dbMutex.ReleaseMutex();
        }

        internal void InsertSession(GameData data, Values v) {
            var s = new Session(v, this._eventId);
            RaceEngineerPlugin.LogInfo(s.ToString());
            //this.InsertSession(s);
            _ = Task.Run(() => this.InsertSession(s));
        }

        internal void InsertSession(Session s) {
            if (this._insertSessionCmd == null) return;

            this._dbMutex.WaitOne();
            try {
                this.SetParam(this._insertSessionCmd, EVENT_ID, s.EventId);
                this.SetParam(this._insertSessionCmd, SESSION_TYPE, s.SessionType);
                this.SetParam(this._insertSessionCmd, TIME_MULTIPLIER, s.TimeMultiplier);
                this.SetParam(this._insertSessionCmd, START_TIME, s.StartTime);
                this._sessionId = (long)this._insertSessionCmd.ExecuteScalar();
            } catch (Exception ex) {
                RaceEngineerPlugin.LogError($"Failed to insert session to DB: {ex}");
            }

            this._dbMutex.ReleaseMutex();
        }

        internal void UpdateSessionTimeMultiplier(int mult) {
            var sessId = this._sessionId;
            _ = Task.Run(() => {
                this._dbMutex.WaitOne();
                try {
                    var cmd = new SQLiteCommand(this._conn) {
                        CommandText = $@"UPDATE {this.sessionsTable.name}
						SET {TIME_MULTIPLIER} = {mult}
						WHERE {SESSION_ID} == {sessId}"
                    };

                    cmd.ExecuteNonQuery();
                } catch (Exception ex) {
                    RaceEngineerPlugin.LogError($"Failed to update session time multiplier to DB: {ex}");
                }
                this._dbMutex.ReleaseMutex();
            });
        }

        internal void InsertStint(GameData data, Values v) {
            var stint = new Stint(data, v, this._sessionId);
            RaceEngineerPlugin.LogInfo(stint.ToString());
            //this.InsertStint(stint);
            _ = Task.Run(() => this.InsertStint(stint));
        }

        private void InsertStint(Stint s) {
            if (this._insertStintCmd == null) return;

            this._dbMutex.WaitOne();
            try {
                this.SetParam(this._insertStintCmd, SESSION_ID, s.SessionId);
                this.SetParam(this._insertStintCmd, STINT_NR, s.StintNr);
                this.SetParam(this._insertStintCmd, START_TIME, s.StartTime);
                this.SetParam(this._insertStintCmd, TYRE_COMPOUND, s.TyreCompound);

                for (var i = 0; i < 4; i++) {
                    this.SetParam(this._insertStintCmd, TYRE_PRES_IN + $"_{TYRES[i]}", s.TyrePresIn[i]);
                }

                this.SetParam(this._insertStintCmd, BRAKE_PAD_FRONT, s.BrakePadFront);
                this.SetParam(this._insertStintCmd, BRAKE_PAD_REAR, s.BrakePadRear);
                this.SetParam(this._insertStintCmd, TYRE_SET, s.TyreSet);
                this.SetParam(this._insertStintCmd, BRAKE_PAD_NR, s.BrakePadNr);
                this.SetParam(this._insertStintCmd, BRAKE_DUCT_FRONT, s.BrakeDuctFront);
                this.SetParam(this._insertStintCmd, BRAKE_DUCT_REAR, s.BrakeDuctRear);
                for (var i = 0; i < 4; i++) {
                    this.SetParam(this._insertStintCmd, CAMBER + $"_{TYRES[i]}", s.Camber[i]);
                    this.SetParam(this._insertStintCmd, TOE + $"_{TYRES[i]}", s.Toe[i]);
                }
                this.SetParam(this._insertStintCmd, CASTER + $"_{TYRES[0]}", s.CasterLf);
                this.SetParam(this._insertStintCmd, CASTER + $"_{TYRES[1]}", s.CasterRf);

                this._stintId = (long)this._insertStintCmd.ExecuteScalar();
            } catch (Exception ex) {
                RaceEngineerPlugin.LogError($"Failed to insert stint to DB: {ex}");
            }
            this._dbMutex.ReleaseMutex();
        }

        internal void InsertLap(GameData data, Values v) {
            var lap = new Lap(data, v, this._stintId);
            RaceEngineerPlugin.LogInfo(lap.ToString());
            //this.InsertLap(lap);
            _ = Task.Run(() => this.InsertLap(lap));
        }

        private void InsertLap(Lap l) {
            if (this._insertLapCmd == null) return;

            this._dbMutex.WaitOne();
            try {
                this.SetParam(this._insertLapCmd, STINT_ID, l.StintId);
                this.SetParam(this._insertLapCmd, SESSION_LAP_NR, l.SessionLapNr);
                this.SetParam(this._insertLapCmd, STINT_LAP_NR, l.StintLapNr);
                this.SetParam(this._insertLapCmd, TYRESET_LAP_NR, l.TyresetLapNr);
                this.SetParam(this._insertLapCmd, BRAKE_PAD_LAP_NR, l.BrakesLapNr);
                this.SetParam(this._insertLapCmd, AIR_TEMP, l.AirTemp);
                this.SetParam(this._insertLapCmd, AIR_TEMP_DELTA, l.AirTempDelta);
                this.SetParam(this._insertLapCmd, TRACK_TEMP, l.TrackTemp);
                this.SetParam(this._insertLapCmd, TRACK_TEMP_DELTA, l.TrackTempDelta);
                this.SetParam(this._insertLapCmd, LAP_TIME, l.LapTime);
                this.SetParam(this._insertLapCmd, FUEL_USED, l.FuelUsed);
                this.SetParam(this._insertLapCmd, FUEL_LEFT, l.FuelLeft);

                for (var i = 0; i < 4; i++) {
                    var tyre = $"_{TYRES[i]}";
                    this.SetParam(this._insertLapCmd, TYRE_PRES_AVG + tyre, l.TyrePresAvg[i]);
                    this.SetParam(this._insertLapCmd, TYRE_PRES_MIN + tyre, l.TyrePresMin[i]);
                    this.SetParam(this._insertLapCmd, TYRE_PRES_MAX + tyre, l.TyrePresMax[i]);
                    this.SetParam(this._insertLapCmd, TYRE_PRES_LOSS + tyre, l.TyrePresLoss[i]);
                    this.SetParam(this._insertLapCmd, TYRE_PRES_LOSS_LAP + tyre, l.TyrePresLossLap[i]);

                    this.SetParam(this._insertLapCmd, TYRE_TEMP_AVG + tyre, l.TyreTempAvg[i]);
                    this.SetParam(this._insertLapCmd, TYRE_TEMP_MIN + tyre, l.TyreTempMin[i]);
                    this.SetParam(this._insertLapCmd, TYRE_TEMP_MAX + tyre, l.TyreTempMax[i]);

                    this.SetParam(this._insertLapCmd, BRAKE_TEMP_AVG + tyre, l.BrakeTempAvg[i]);
                    this.SetParam(this._insertLapCmd, BRAKE_TEMP_MIN + tyre, l.BrakeTempMin[i]);
                    this.SetParam(this._insertLapCmd, BRAKE_TEMP_MAX + tyre, l.BrakeTempMax[i]);

                    this.SetParam(this._insertLapCmd, TYRE_LIFE_LEFT + tyre, 0.0);
                }

                this.SetParam(this._insertLapCmd, ABS, l.Abs);
                this.SetParam(this._insertLapCmd, TC, l.Tc);
                this.SetParam(this._insertLapCmd, ECU_MAP, l.EcuMap);
                this.SetParam(this._insertLapCmd, ECU_MAP_CHANGED, l.EcuMapChanged);

                this.SetParam(this._insertLapCmd, TC2, l.Tc2);
                this.SetParam(this._insertLapCmd, TRACK_GRIP_STATUS, l.TrackGripStatus);

                for (var i = 0; i < 4; i++) {
                    this.SetParam(this._insertLapCmd, PAD_LIFE_LEFT + $"_{TYRES[i]}", l.PadLifeLeft[i]);
                    this.SetParam(this._insertLapCmd, DISC_LIFE_LEFT + $"_{TYRES[i]}", l.DiscLifeLeft[i]);
                }

                this.SetParam(this._insertLapCmd, IS_VALID, l.IsValid);
                // Need to use booleans.OldData which is the last point on finished lap
                this.SetParam(this._insertLapCmd, IS_VALID_FUEL_LAP, l.IsValidFuelLap);
                this.SetParam(this._insertLapCmd, IS_OUTLAP, l.IsOutLap);
                this.SetParam(this._insertLapCmd, IS_INLAP, l.IsInLap);

                this.SetParam(this._insertLapCmd, RAIN_INTENSITY, l.RainIntensity);
                this.SetParam(this._insertLapCmd, RAIN_INTENSITY_CHANGED, l.RainIntensityChanged);

                this._insertLapCmd.ExecuteNonQuery();
            } catch (Exception ex) {
                RaceEngineerPlugin.LogError($"Failed to insert lap to DB: {ex}");
            }
            this._dbMutex.ReleaseMutex();
        }
        #endregion

        #region QUERIES

        internal List<PrevData> GetPrevSessionData(GameData data, Values v) {
            int trackGrip;
            int rainIntensity;
            if (RaceEngineerPlugin.Game.IsAcc) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();

                trackGrip = (int)rawDataNew.Graphics.trackGripStatus;
                rainIntensity = (int)rawDataNew.Graphics.rainIntensity;
            } else {
                trackGrip = (int)ACC_TRACK_GRIP_STATUS.ACC_OPTIMUM;
                rainIntensity = (int)ACC_RAIN_INTENSITY.ACC_NO_RAIN;
            }
            string conds = $"AND l.{IS_VALID} AND l.{TRACK_GRIP_STATUS} IN ";
            if (0 < trackGrip && trackGrip < 3) {
                conds += "(0, 1, 2)";
            } else {
                conds += $"({trackGrip})";
            }

            List<PrevData> list = new List<PrevData>(RaceEngineerPlugin.Settings.NumPreviousValuesStored);
            this._dbMutex.WaitOne();
            try {


                var cmd = new SQLiteCommand(this._conn) {
                    CommandText = $@"SELECT l.{LAP_TIME}, l.{FUEL_USED} FROM {this.lapsTable.name} AS l 
					INNER JOIN {this.stintsTable.name} AS s ON l.{STINT_ID} == s.{STINT_ID} 
					INNER JOIN {this.sessionsTable.name} AS sess ON s.{SESSION_ID} == sess.{SESSION_ID} 
					INNER JOIN {this.eventsTable.name} AS e ON e.{EVENT_ID} == sess.{EVENT_ID} 
					WHERE 
						e.{CAR_ID} == '{v.Car.Name}' 
						AND e.{TRACK_ID} == '{v.Track.Name}' 
						{conds} 
						AND l.{RAIN_INTENSITY} == {rainIntensity} 
						AND l.{RAIN_INTENSITY_CHANGED} == 0
					ORDER BY l.{LAP_ID} DESC
					LIMIT {RaceEngineerPlugin.Settings.NumPreviousValuesStored}"
                };

                SQLiteDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read()) {
                    if (this.HasNullFields(rdr)) continue;
                    list.Add(new PrevData(rdr.GetDouble(0), rdr.GetDouble(1)));
                }
                rdr.Close();
            } catch (Exception ex) {
                RaceEngineerPlugin.LogError($"Failed to read previous session data from DB: {ex}");
            }
            this._dbMutex.ReleaseMutex();
            return list;
        }

        private const int LAP_NR_LOW_THRESHOLD = 2;
        private const int LAP_NR_HIGH_THRESHOLD = 11;
        private const double TYRE_PRES_LOSS_THRESHOLD = 0.25;
        private const double AIR_TEMP_CHANGE_THRESHOLD = 0.25;
        private const double TRACK_TEMP_CHANGE_THRESHOLD = 0.25;
        internal Tuple<List<double[]>, List<double>> GetInputPresData(int tyre, string car, string track, int brakeDuct, string compound, string trackGrip, ACC_RAIN_INTENSITY rainIntensity) {
            string duct;
            if (tyre < 2) {
                duct = BRAKE_DUCT_FRONT;
            } else {
                duct = BRAKE_DUCT_REAR;
            }

            var ty = TYRES[tyre];

            List<double> y = [];
            List<double[]> x = [];

            this._dbMutex.WaitOne();
            try {

                var cmd = new SQLiteCommand(this._conn) {
                    CommandText = $@"
					SELECT s.{TYRE_PRES_IN}_{ty}, l.{TYRE_PRES_AVG}_{ty}, l.{TYRE_PRES_LOSS}_{ty}, l.{AIR_TEMP}, l.{TRACK_TEMP} FROM {this.lapsTable.name} AS l
					INNER JOIN {this.stintsTable.name} AS s ON l.{STINT_ID} == s.{STINT_ID} 
					INNER JOIN {this.sessionsTable.name} AS sess ON s.{SESSION_ID} == sess.{SESSION_ID} 
					INNER JOIN {this.eventsTable.name} AS e ON e.{EVENT_ID} == sess.{EVENT_ID} 
					WHERE e.car_id == '{car}' 
						AND e.track_id == '{track}' 
						AND l.stint_lap_nr > {LAP_NR_LOW_THRESHOLD} 
						AND l.stint_lap_nr < {LAP_NR_HIGH_THRESHOLD} 
						AND s.{TYRE_COMPOUND} == '{compound}'
						AND l.{TYRE_PRES_LOSS}_{ty} > -{TYRE_PRES_LOSS_THRESHOLD}
						AND l.{AIR_TEMP_DELTA} < {AIR_TEMP_CHANGE_THRESHOLD} AND l.{AIR_TEMP_DELTA} > -{AIR_TEMP_CHANGE_THRESHOLD}
						AND l.{TRACK_TEMP_DELTA} < {TRACK_TEMP_CHANGE_THRESHOLD} AND l.{TRACK_TEMP_DELTA} > -{TRACK_TEMP_CHANGE_THRESHOLD}
						AND l.{TYRE_PRES_LOSS_LAP}_{ty} == 0
						AND l.{RAIN_INTENSITY_CHANGED} == 0
						AND l.{RAIN_INTENSITY} == {(int)rainIntensity}"
                };
                if (-1 < brakeDuct && brakeDuct < 7) {
                    cmd.CommandText += $" AND s.{duct} == {brakeDuct}";
                }
                if (trackGrip != null) {
                    cmd.CommandText += $" AND l.{TRACK_GRIP_STATUS} in {trackGrip}";
                }

                SQLiteDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read()) {
                    if (this.HasNullFields(rdr)) continue;

                    y.Add(rdr.GetDouble(0));
                    // Homogeneous coordinate, avg_press - loss, air_temp, track_temp
                    x.Add([1.0, rdr.GetDouble(1) - rdr.GetDouble(2), rdr.GetDouble(3), rdr.GetDouble(4)]);
                }
                rdr.Close();
            } catch (Exception ex) {
                RaceEngineerPlugin.LogError($"Failed to read input press data from DB: {ex}");
            }
            this._dbMutex.ReleaseMutex();

            RaceEngineerPlugin.LogInfo($"Read {y.Count} datapoints for {ty} tyre pressure model with");

            return Tuple.Create(x, y);
        }

        private bool HasNullFields(SQLiteDataReader rdr) {
            for (int i = 0; i < rdr.FieldCount; i++) {
                if (rdr.IsDBNull(i)) return true;
            }
            return false;
        }

        #endregion

    }

    /// <summary>
    /// Definition of database table and methods to create some methods
    /// </summary>
    class DBTable(string name, DBField[] fields) {
        internal string name = name;
        internal DBField[] fields = fields;

        internal void CreateTableWForeignKey(SQLiteConnection conn, string foreignKey) {
            var cmd = new SQLiteCommand(conn);

            List<string> fields = new List<string>(this.fields.Length);
            foreach (var f in this.fields) {
                fields.Add(f.name + " " + f.type);
            }

            cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS {this.name} ({String.Join(", ", fields)}, {foreignKey})";
            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }

        internal void CreateTable(SQLiteConnection conn) {
            var cmd = new SQLiteCommand(conn);

            List<string> fields = new List<string>(this.fields.Length);
            foreach (var f in this.fields) {
                fields.Add(f.name + " " + f.type);
            }

            cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS {this.name} ({String.Join(", ", fields)})";
            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }

        internal SQLiteCommand CreateInsertCmd(SQLiteConnection conn) {
            List<string> fields = new List<string>(this.fields.Length - 1);
            List<string> atfields = new List<string>(this.fields.Length - 1);
            foreach (var f in this.fields) {
                if (f.type == "INTEGER PRIMARY KEY") {
                    continue;
                }
                fields.Add(f.name);
                atfields.Add("@" + f.name);
            }

            var cmd = new SQLiteCommand(conn);
            cmd.CommandText = $@"INSERT INTO {this.name}({String.Join(", ", fields)}) VALUES({String.Join(", ", atfields)})";

            foreach (var f in this.fields) {
                if (f.type == "TEXT") {
                    cmd.Parameters.AddWithValue("@" + f.name, "");
                } else if (f.type == "INTEGER") {
                    cmd.Parameters.AddWithValue("@" + f.name, 0);
                } else {
                    cmd.Parameters.AddWithValue("@" + f.name, 0.0);
                }
            }

            return cmd;
        }

        internal SQLiteCommand CreateInsertCmdWReturning(SQLiteConnection conn, string returning_field) {
            List<string> fields = new List<string>(this.fields.Length - 1);
            List<string> atfields = new List<string>(this.fields.Length - 1);
            foreach (var f in this.fields.Skip(1)) {
                if (f.type == "INTEGER PRIMARY KEY") {
                    continue;
                }
                fields.Add(f.name);
                atfields.Add("@" + f.name);
            }

            var cmd = new SQLiteCommand(conn);
            cmd.CommandText = $@"INSERT INTO {this.name}({String.Join(", ", fields)}) VALUES({String.Join(", ", atfields)}) returning {returning_field}";

            foreach (var f in this.fields) {
                if (f.type == "TEXT") {
                    cmd.Parameters.AddWithValue("@" + f.name, "");
                } else if (f.type == "INTEGER") {
                    cmd.Parameters.AddWithValue("@" + f.name, 0);
                } else {
                    cmd.Parameters.AddWithValue("@" + f.name, 0.0);
                }
            }

            return cmd;
        }
    }

    class DBField(string name, string type) {
        internal string name = name;
        internal string type = type;
    }

    internal class PrevData(double lapTime, double fuelUsed) {
        internal double lapTime = lapTime;
        internal double fuelUsed = fuelUsed;
    }
}