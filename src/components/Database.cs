﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ACSharedMemory.ACC.MMFModels;
using GameReaderCommon;
using SimHub.Plugins;
using RaceEngineerPlugin.Car;

namespace RaceEngineerPlugin.Database
{
	public class Stint {
		public long EventId = 0;
		public string SessionType = null;
		public int StintNr;
		public string StartTime;
		public string TyreCompound;
		public double[] TyrePresIn = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public int BrakePadFront;
		public int BrakePadReat;
		public int BrakePadNr;
		public int BrakeDuctFront;
		public int BrakeDuctRear;
		public int TyreSet;
		public int[] Camber = new int[4] { 0, 0, 0, 0 };
		public int[] Toe = new int[4] { 0, 0, 0, 0 };
		public int CasterLf;
		public int CasterRf;

		public Stint() { }

		public Stint(GameData data, Values v, long eventId) {
			Update(data, v, eventId);
		}

		public void Update(GameData data, Values v, long eventId) {
			this.EventId = eventId;
			string stime = DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss");

			string sessType = v.Session.RaceSessionType.ToString();
			SessionType = sessType;
			StintNr = v.Laps.StintNr;
			StartTime = stime;
			TyreCompound = v.Car.Tyres.Name;

			for (var i = 0; i < 4; i++) {
				TyrePresIn[i] = v.Car.Tyres.CurrentInputPres[i];
			}

			if (RaceEngineerPlugin.GAME.IsAcc) {
				BrakePadFront = (int)v.RawData.NewData.Physics.frontBrakeCompound + 1;
				BrakePadReat = (int)v.RawData.NewData.Physics.rearBrakeCompound + 1;
				TyreSet = v.Car.Tyres.CurrentTyreSet;
			} else {
				BrakePadFront = -1;
				BrakePadReat = -1;
				TyreSet = -1;
			}

			BrakePadNr = v.Car.Brakes.SetNr;

			if (v.Car.Setup != null) {
				BrakeDuctFront = v.Car.Setup.advancedSetup.aeroBalance.brakeDuct[0];
				BrakeDuctRear = v.Car.Setup.advancedSetup.aeroBalance.brakeDuct[1];
				for (var i = 0; i < 4; i++) {
					Camber[i] = v.Car.Setup.basicSetup.alignment.camber[i];
					Toe[i] = v.Car.Setup.basicSetup.alignment.toe[i];
				}

				CasterLf = v.Car.Setup.basicSetup.alignment.casterLF;
				CasterRf = v.Car.Setup.basicSetup.alignment.casterRF;
			} else {
				BrakeDuctFront = -1;
				BrakeDuctRear = -1;
				for (var i = 0; i < 4; i++) {
					Camber[i] = -1;
					Toe[i] = -1;
				}

				CasterLf = -1;
				CasterRf = -1;
			}

		}
	}

	public class Lap {
		public long StintId;
		public int SessionLapNr;
		public int StintLapNr;
		public int TyresetLapNr;
		public int BrakesLapNr;
		public double AirTemp;
		public double AirTempDelta;
		public double TrackTemp;
		public double TrackTempDelta;
		public double LapTime;
		public double FuelUsed;
		public double FuelLeft;

		public double[] TyrePresAvg = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] TyrePresMin = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] TyrePresMax = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] TyrePresLoss = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public bool[] TyrePressLossLap = new bool[4] { false, false, false, false };

		public double[] TyreTempAvg = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] TyreTempMin = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] TyreTempMax = new double[4] { 0.0, 0.0, 0.0, 0.0 };

		public double[] BrakeTempAvg = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] BrakeTempMin = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] BrakeTempMax = new double[4] { 0.0, 0.0, 0.0, 0.0 };

		public double[] TyreLifeLeft = new double[4] { 0.0, 0.0, 0.0, 0.0 };

		public double[] PadLifeLeft = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] DiscLifeLeft = new double[4] { 0.0, 0.0, 0.0, 0.0 };

		public int Abs;
		public int Tc;
		public int Tc2;
		public int EcuMap;
		public bool EcuMapChanged;
		public int TrackGripStatus;
		public bool IsValid;
		public bool IsValidFuelLap;
		public bool IsOutLap;
		public bool IsInLap;
		public int RainIntensity;
		public bool RainIntensityChanged;

		public Lap() { }


		public Lap(GameData data, Values v, long stint_id) {
			Update(data, v, stint_id);
		}


		public void Update(GameData data, Values v, long stint_id) {
			this.StintId = stint_id;
			SessionLapNr = data.NewData.CompletedLaps;
			StintLapNr = v.Laps.StintLaps;
			if (RaceEngineerPlugin.GAME.IsAcc) {
				TyresetLapNr = v.Car.Tyres.GetCurrentSetLaps();
			} else {
				TyresetLapNr = 0;
			}
			BrakesLapNr = v.Car.Brakes.LapsNr;
			AirTemp = data.NewData.AirTemperature;
			AirTempDelta = data.NewData.AirTemperature - v.Weather.AirTempAtLapStart;
			TrackTemp = data.NewData.RoadTemperature;
			TrackTempDelta = data.NewData.RoadTemperature - v.Weather.TrackTempAtLapStart;
			LapTime = v.Laps.LastTime;
			FuelUsed = v.Car.Fuel.LastUsedPerLap;
			FuelLeft = v.Car.Fuel.Remaining;

			for (var i = 0; i < 4; i++) {
				TyrePresAvg[i] = v.Car.Tyres.PresOverLap[i].Avg;
				TyrePresMin[i] = v.Car.Tyres.PresOverLap[i].Min;
				TyrePresMax[i] = v.Car.Tyres.PresOverLap[i].Max;
				TyrePresLoss[i] = v.Car.Tyres.PresLoss[i];
				TyrePressLossLap[i] = v.Car.Tyres.PresLossLap[i];

				TyreTempAvg[i] = v.Car.Tyres.TempOverLap[i].Avg;
				TyreTempMin[i] = v.Car.Tyres.TempOverLap[i].Min;
				TyreTempMax[i] = v.Car.Tyres.TempOverLap[i].Max;

				BrakeTempAvg[i] = v.Car.Brakes.TempOverLap[i].Avg;
				BrakeTempMin[i] = v.Car.Brakes.TempOverLap[i].Min;
				BrakeTempMax[i] = v.Car.Brakes.TempOverLap[i].Max;
			}

			Abs = data.NewData.ABSLevel;
			Tc = data.NewData.TCLevel;
			EcuMap = data.NewData.EngineMap;
			EcuMapChanged = v.Booleans.OldData.EcuMapChangedThisLap;

			if (RaceEngineerPlugin.GAME.IsAcc) {
				Tc2 = v.RawData.NewData.Graphics.TCCut;

				for (var i = 0; i < 4; i++) {
					PadLifeLeft[i] = (float)v.RawData.NewData.Physics.padLife[i];
					DiscLifeLeft[i] = (float)v.RawData.NewData.Physics.discLife[i];
				}

				TrackGripStatus = (int)v.RawData.NewData.Graphics.trackGripStatus;
			} else {
				Tc2 = -1;
				for (var i = 0; i < 4; i++) {
					PadLifeLeft[i] = -1;
				}

				TrackGripStatus = -1;
			}

			IsValid = v.Booleans.NewData.SavePrevLap;
			// Need to use booleans.OldData which is the last point on finished lap
			IsValidFuelLap = v.Booleans.OldData.IsValidFuelLap;
			IsOutLap = v.Booleans.OldData.IsOutLap;
			IsInLap = v.Booleans.OldData.IsInLap;

			RainIntensity = (int)v.RawData.NewData.Graphics.rainIntensity;
			RainIntensityChanged = v.Booleans.OldData.RainIntensityChangedThisLap;
		}
	}


	/// <summary>
	/// Handles data collection/storing for plugin.
	/// </summary>
	public class Database : IDisposable {
		private SQLiteConnection _conn;

		private SQLiteCommand _insertEventCmd;
		private SQLiteCommand _insertStintCmd;
		private SQLiteCommand _insertLapCmd;

		private long _eventId;
		private long _stintId;
		private Mutex _dbMutex = new Mutex();

		public Database() {
			var location = $@"{RaceEngineerPlugin.SETTINGS.DataLocation}\{RaceEngineerPlugin.GAME.Name}\data.db";
			if (!File.Exists(location)) {
				Directory.CreateDirectory(Path.GetDirectoryName(location));
			}
			_conn = new SQLiteConnection($"Data Source={location}");
			try {
				_conn.Open();
				eventsTable.CreateTable(_conn);
				stintsTable.CreateTableWForeignKey(_conn, $"FOREIGN KEY({EVENT_ID}) REFERENCES {eventsTable.name}({EVENT_ID})");
				lapsTable.CreateTableWForeignKey(_conn, $"FOREIGN KEY({STINT_ID}) REFERENCES {stintsTable.name}({STINT_ID})");
			
				_insertEventCmd = eventsTable.CreateInsertCmdWReturning(_conn, EVENT_ID);
				_insertStintCmd = stintsTable.CreateInsertCmdWReturning(_conn, STINT_ID);
				_insertLapCmd = lapsTable.CreateInsertCmd(_conn);

				RaceEngineerPlugin.LogInfo($"Opened database from '{location}'");
			} catch (Exception ex) {
				RaceEngineerPlugin.LogInfo($"Failed to open DB. location={location} Error msq: {ex}");
			}
		}

		#region IDisposable
		~Database() { 
			Dispose(false);
		}
		
		private bool isDisposed = false;
		protected virtual void Dispose(bool disposing) {
			if (!isDisposed) {
				_dbMutex.WaitOne();
				
				if (disposing) {
					_insertEventCmd.Dispose();
					_insertStintCmd.Dispose();
					_insertLapCmd.Dispose();
				}

				if (_conn != null) {
					_conn.Close();
					_conn.Dispose();
					_conn = null;
				}

				RaceEngineerPlugin.LogInfo("Disposed.");
				isDisposed = true;
				_dbMutex.ReleaseMutex();
				_dbMutex.Dispose();
				_dbMutex = null;
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		#region TABLE DEFINITIONS

		private const string EVENT_ID = "event_id";
		private const string CAR_ID = "car_id";
		private const string TRACK_ID = "track_id";
		private const string START_TIME = "start_time";

		private DBTable eventsTable = new DBTable("events", new DBField[] {
			new DBField(EVENT_ID, "INTEGER PRIMARY KEY"),
			new DBField(CAR_ID, "TEXT"),
			new DBField(TRACK_ID, "TEXT"),
			new DBField(START_TIME, "TEXT")
		});

		private static string[] TYRES = new string[] { Tyres.Names[0].ToLower(), Tyres.Names[1].ToLower(), Tyres.Names[2].ToLower(), Tyres.Names[3].ToLower() };
		private const string STINT_ID = "stint_id";
		private const string SESSION_TYPE = "session_type";
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

		private DBTable stintsTable = new DBTable("stints", new DBField[] {
			new DBField(STINT_ID, "INTEGER PRIMARY KEY"),
			new DBField(EVENT_ID, "INTEGER"),
			new DBField(SESSION_TYPE, "TEXT"),
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
		});

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

		private DBTable lapsTable = new DBTable("laps", new DBField[] {
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
		});

        #endregion

        #region INSERTS

        private void SetParam(SQLiteCommand cmd, string name, object value) {
			cmd.Parameters["@" + name].Value = value.ToString();
		}

		private void SetParam(SQLiteCommand cmd, string name, bool value) {
			SetParam(cmd, name, value ? 1 : 0);
		}

		public void InsertEvent(string carName, string trackName) {
			_dbMutex.WaitOne();
			
			string stime = DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss");

			SetParam(_insertEventCmd, CAR_ID, carName);
			SetParam(_insertEventCmd, TRACK_ID, trackName);
			SetParam(_insertEventCmd, START_TIME, stime);

			_eventId = (long)_insertEventCmd.ExecuteScalar();
			_numCommands++;

            var debugCmd = new SQLiteCommand(_conn) {
                CommandText = $"SELECT * FROM {eventsTable.name} ORDER BY rowid DESC LIMIT 1"
            };
            var rdr = debugCmd.ExecuteReader();
			rdr.Read();

			var txt = $"Inserted event @ {stime}";
			for (var i = 0; i < rdr.FieldCount; i++) {
				txt += $"\n\t{rdr.GetName(i)} = {rdr.GetValue(i)}";
			}
			rdr.Close();
			_dbMutex.ReleaseMutex();

			RaceEngineerPlugin.LogInfo(txt);
		}

		public void InsertStint(GameData data, Values v) {
			var stint = new Stint(data, v, _eventId);
			_ = Task.Run(() => InsertStint(stint));
		}

		private void InsertStint(Stint s) {
			_dbMutex.WaitOne();

			SetParam(_insertStintCmd, EVENT_ID, s.EventId);
			SetParam(_insertStintCmd, SESSION_TYPE, s.SessionType);
			SetParam(_insertStintCmd, STINT_NR, s.StintNr);
			SetParam(_insertStintCmd, START_TIME, s.StartTime);
			SetParam(_insertStintCmd, TYRE_COMPOUND, s.TyreCompound);

			for (var i = 0; i < 4; i++) {
				SetParam(_insertStintCmd, TYRE_PRES_IN + $"_{TYRES[i]}", s.TyrePresIn[i]);
			}

			SetParam(_insertStintCmd, BRAKE_PAD_FRONT, s.BrakePadFront);
			SetParam(_insertStintCmd, BRAKE_PAD_REAR, s.BrakePadReat);
			SetParam(_insertStintCmd, TYRE_SET, s.TyreSet);
			SetParam(_insertStintCmd, BRAKE_PAD_NR, s.BrakePadNr);
			SetParam(_insertStintCmd, BRAKE_DUCT_FRONT, s.BrakeDuctFront);
			SetParam(_insertStintCmd, BRAKE_DUCT_REAR, s.BrakeDuctRear);
			for (var i = 0; i < 4; i++) {
				SetParam(_insertStintCmd, CAMBER + $"_{TYRES[i]}", s.Camber[i]);
				SetParam(_insertStintCmd, TOE + $"_{TYRES[i]}", s.Toe[i]);
			}
			SetParam(_insertStintCmd, CASTER + $"_{TYRES[0]}", s.CasterLf);
			SetParam(_insertStintCmd, CASTER + $"_{TYRES[1]}", s.CasterRf);

			_stintId = (long)_insertStintCmd.ExecuteScalar();
			//insertStintCmd.Reset();
			_numCommands++;

			// Debug
			var debugCmd = new SQLiteCommand(_conn);
			debugCmd.CommandText = $"SELECT * FROM {stintsTable.name} ORDER BY rowid DESC LIMIT 1";
			var rdr = debugCmd.ExecuteReader();
			rdr.Read();

			Func<int, string> fmt = i => { 
				var type = rdr.GetDataTypeName(i);
				var value = rdr.GetValue(i);
				if (type == "REAL") {
					return $"{value:0.000}";
				} else {
					return $"{value}";
				}
			};

			var txt = $"Inserted stint @ {DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss")}";
			for (var i = 0; i < rdr.FieldCount; i++) {
				var cname = rdr.GetName(i);
				if (cname.EndsWith(TYRES[0]) && !cname.StartsWith("caster")) {
					txt += $"\n\t{cname} = [{fmt(i)}, {fmt(i + 1)}, {fmt(i + 2)}, {fmt(i + 3)}]";
					i += 3;
				} else if (cname == CASTER + $"_{TYRES[0]}" || cname == BRAKE_PAD_FRONT || cname == BRAKE_DUCT_FRONT) {
					txt += $"\n\t{cname} = [{fmt(i)}, {fmt(i + 1)}]";
					i += 1;
				} else {
					txt += $"\n\t{cname} = {fmt(i)}";
				}
			}
			rdr.Close();
			_dbMutex.ReleaseMutex();

			RaceEngineerPlugin.LogInfo(txt);
		}

		public void InsertLap(GameData data, Values v) {
			var lap = new Lap(data, v, _stintId);
			_ = Task.Run(() => InsertLap(lap));
		}

		private void InsertLap(Lap l) {
			_dbMutex.WaitOne();

			SetParam(_insertLapCmd, STINT_ID, l.StintId);
			SetParam(_insertLapCmd, SESSION_LAP_NR, l.SessionLapNr);
			SetParam(_insertLapCmd, STINT_LAP_NR, l.StintLapNr);
			SetParam(_insertLapCmd, TYRESET_LAP_NR, l.TyresetLapNr);
			SetParam(_insertLapCmd, BRAKE_PAD_LAP_NR, l.BrakesLapNr);
			SetParam(_insertLapCmd, AIR_TEMP, l.AirTemp);
			SetParam(_insertLapCmd, AIR_TEMP_DELTA, l.AirTempDelta);
			SetParam(_insertLapCmd, TRACK_TEMP, l.TrackTemp);
			SetParam(_insertLapCmd, TRACK_TEMP_DELTA, l.TrackTempDelta);
			SetParam(_insertLapCmd, LAP_TIME, l.LapTime);
			SetParam(_insertLapCmd, FUEL_USED, l.FuelUsed);
			SetParam(_insertLapCmd, FUEL_LEFT, l.FuelLeft);

			for (var i = 0; i < 4; i++) {
				var tyre = $"_{TYRES[i]}";
				SetParam(_insertLapCmd, TYRE_PRES_AVG + tyre, l.TyrePresAvg[i]);
				SetParam(_insertLapCmd, TYRE_PRES_MIN + tyre, l.TyrePresMin[i]);
				SetParam(_insertLapCmd, TYRE_PRES_MAX + tyre, l.TyrePresMax[i]);
				SetParam(_insertLapCmd, TYRE_PRES_LOSS + tyre, l.TyrePresLoss[i]);
				SetParam(_insertLapCmd, TYRE_PRES_LOSS_LAP + tyre, l.TyrePressLossLap[i]);

				SetParam(_insertLapCmd, TYRE_TEMP_AVG + tyre, l.TyreTempAvg[i]);
				SetParam(_insertLapCmd, TYRE_TEMP_MIN + tyre, l.TyreTempMin[i]);
				SetParam(_insertLapCmd, TYRE_TEMP_MAX + tyre, l.TyreTempMax[i]);

				SetParam(_insertLapCmd, BRAKE_TEMP_AVG + tyre, l.BrakeTempAvg[i]);
				SetParam(_insertLapCmd, BRAKE_TEMP_MIN + tyre, l.BrakeTempMin[i]);
				SetParam(_insertLapCmd, BRAKE_TEMP_MAX + tyre, l.BrakeTempMax[i]);

				SetParam(_insertLapCmd, TYRE_LIFE_LEFT + tyre, 0.0);
			}

			SetParam(_insertLapCmd, ABS, l.Abs);
			SetParam(_insertLapCmd, TC, l.Tc);
			SetParam(_insertLapCmd, ECU_MAP, l.EcuMap);
			SetParam(_insertLapCmd, ECU_MAP_CHANGED, l.EcuMapChanged);

			SetParam(_insertLapCmd, TC2, l.Tc2);
			SetParam(_insertLapCmd, TRACK_GRIP_STATUS, l.TrackGripStatus);

			for (var i = 0; i < 4; i++) {
				SetParam(_insertLapCmd, PAD_LIFE_LEFT + $"_{TYRES[i]}", l.PadLifeLeft[i]);
				SetParam(_insertLapCmd, DISC_LIFE_LEFT + $"_{TYRES[i]}", l.DiscLifeLeft[i]);
			}

			SetParam(_insertLapCmd, IS_VALID, l.IsValid);
			// Need to use booleans.OldData which is the last point on finished lap
			SetParam(_insertLapCmd, IS_VALID_FUEL_LAP, l.IsValidFuelLap);
			SetParam(_insertLapCmd, IS_OUTLAP, l.IsOutLap);
			SetParam(_insertLapCmd, IS_INLAP, l.IsInLap);

			SetParam(_insertLapCmd, RAIN_INTENSITY, l.RainIntensity);
			SetParam(_insertLapCmd, RAIN_INTENSITY_CHANGED, l.RainIntensityChanged);


			_insertLapCmd.ExecuteNonQuery();
			_numCommands++;


            // Debug log inserted values
            var debugCmd = new SQLiteCommand(_conn);
            debugCmd.CommandText = $"SELECT * FROM {lapsTable.name} ORDER BY rowid DESC LIMIT 1";
            var rdr = debugCmd.ExecuteReader();
            rdr.Read();

            Func<int, string> fmt = i => {
                var type = rdr.GetDataTypeName(i);
                var value = rdr.GetValue(i);
                if (type == "REAL") {
                    return $"{value:0.000}";
                } else {
                    return $"{value}";
                }
            };

            var txt = $"Inserted lap @ {DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss")}";
            for (var i = 0; i < rdr.FieldCount; i++) {
                var cname = rdr.GetName(i);
                if (cname.EndsWith(TYRES[0])) {
                    txt += $"\n\t{cname} = [{fmt(i)}, {fmt(i + 1)}, {fmt(i + 2)}, {fmt(i + 3)}]";
                    i += 3;
                } else {
                    txt += $"\n\t{cname} = {fmt(i)}";
                }
            }
			rdr.Close();
			_dbMutex.ReleaseMutex();

			RaceEngineerPlugin.LogInfo(txt);

        }
		#endregion

		#region QUERIES

		public List<PrevData> GetPrevSessionData(Values v) {
			var trackGrip = (int)v.RawData.NewData.Graphics.trackGripStatus;
			string conds = $"AND l.{IS_VALID} AND l.{TRACK_GRIP_STATUS} IN ";
			if (0 < trackGrip && trackGrip < 3) {
				conds += "('Green', 'Fast', 'Optimum')";
			} else {
				conds += $"('{trackGrip}')";
			}

			List<PrevData> list = new List<PrevData>(RaceEngineerPlugin.SETTINGS.NumPreviousValuesStored);
			_dbMutex.WaitOne();

			var cmd = new SQLiteCommand(_conn) {
                CommandText = $@"SELECT l.{LAP_TIME}, l.{FUEL_USED} FROM {lapsTable.name} AS l 
					INNER JOIN {stintsTable.name} AS s ON l.{STINT_ID} == s.{STINT_ID} 
					INNER JOIN {eventsTable.name} AS e ON e.{EVENT_ID} == s.{EVENT_ID} 
					WHERE 
						e.{CAR_ID} == '{v.Car.Name}' 
						AND e.{TRACK_ID} == '{v.Track.Name}' 
						{conds} 
						AND l.{RAIN_INTENSITY} == {(int)v.RawData.NewData.Graphics.rainIntensity} 
						AND l.{RAIN_INTENSITY_CHANGED} == 0
					ORDER BY l.{LAP_ID} DESC
					LIMIT {RaceEngineerPlugin.SETTINGS.NumPreviousValuesStored}"
            };

			SQLiteDataReader rdr = cmd.ExecuteReader();

			while (rdr.Read()) {
				if (HasNullFields(rdr)) continue;
				list.Add(new PrevData(rdr.GetDouble(0), rdr.GetDouble(1)));
			}
			rdr.Close();
			_dbMutex.ReleaseMutex();
			return list;
		}

		private const int LAP_NR_LOW_THRESHOLD = 2;
		private const int LAP_NR_HIGH_THRESHOLD = 11;
		private const double TYRE_PRES_LOSS_THRESHOLD = 0.25;
		private const double AIR_TEMP_CHANGE_THRESHOLD = 0.25;
		private const double TRACK_TEMP_CHANGE_THRESHOLD = 0.25;
		public Tuple<List<double[]>, List<double>> GetInputPresData(int tyre, string car, string track, int brakeDuct, string compound, string trackGrip, ACC_RAIN_INTENSITY rainIntensity) {
			string duct;
			if (tyre < 2) {
				duct = BRAKE_DUCT_FRONT;
			} else {
				duct = BRAKE_DUCT_REAR;
			}

			var ty = TYRES[tyre];

			List<double> y = new List<double>();
			List<double[]> x = new List<double[]>();

			_dbMutex.WaitOne();

			var cmd = new SQLiteCommand(_conn) {
                CommandText = $@"
					SELECT s.{TYRE_PRES_IN}_{ty}, l.{TYRE_PRES_AVG}_{ty}, l.{TYRE_PRES_LOSS}_{ty}, l.{AIR_TEMP}, l.{TRACK_TEMP} FROM {lapsTable.name} AS l
					INNER JOIN {stintsTable.name} AS s ON l.{STINT_ID} == s.{STINT_ID} 
					INNER JOIN {eventsTable.name} AS e ON e.{EVENT_ID} == s.{EVENT_ID} 
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
				if (HasNullFields(rdr)) continue;

				y.Add(rdr.GetDouble(0));
				// Homogeneous coordinate, avg_press - loss, air_temp, track_temp
				x.Add(new double[] { 1.0, rdr.GetDouble(1) - rdr.GetDouble(2), rdr.GetDouble(3), rdr.GetDouble(4) });
			}
			rdr.Close();
			_dbMutex.ReleaseMutex();
			RaceEngineerPlugin.LogInfo($"Read {y.Count} datapoints for {ty} tyre pressure model with brake duct {brakeDuct} and compount ");

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
	class DBTable {
		public string name;
		public DBField[] fields;

		public DBTable(string name, DBField[] fields) {
			this.name = name;
			this.fields = fields;
		}

		public void CreateTableWForeignKey(SQLiteConnection conn, string foreignKey) {
			var cmd = new SQLiteCommand(conn);

			List<string> fields = new List<string>(this.fields.Length);
			foreach (var f in this.fields) {
				fields.Add(f.name + " " + f.type);
			}

			cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS {this.name} ({String.Join(", ", fields)}, {foreignKey})";
			cmd.ExecuteNonQuery();
			cmd.Dispose();
		}

		public void CreateTable(SQLiteConnection conn) {
			var cmd = new SQLiteCommand(conn);

			List<string> fields = new List<string>(this.fields.Length);
			foreach (var f in this.fields) {
				fields.Add(f.name + " " + f.type);
			}

			cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS {this.name} ({String.Join(", ", fields)})";
			cmd.ExecuteNonQuery();
			cmd.Dispose();
		}

		public SQLiteCommand CreateInsertCmd(SQLiteConnection conn) {
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

		public SQLiteCommand CreateInsertCmdWReturning(SQLiteConnection conn, string returning_field) {
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

	class DBField {
		public string name;
		public string type;

		public DBField(string name, string type) {
			this.name = name;
			this.type = type;
		}
	}

	public class PrevData {
		public double lapTime;
		public double fuelUsed;

		public PrevData(double lapTime, double fuelUsed) {
			this.lapTime = lapTime;
			this.fuelUsed = fuelUsed;
		}
	}
}