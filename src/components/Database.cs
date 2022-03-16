using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GameReaderCommon;
using SimHub.Plugins;

namespace RaceEngineerPlugin.Database
{
	public class Stint {
		public string session_type = null;
		public int stint_nr;
		public string start_time;
		public string tyre_compound;
		public double[] tyre_pres_in = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public int brake_pad_front;
		public int brake_pad_rear;
		public int brake_pad_nr;
		public int brake_duct_front;
		public int brake_duct_rear;
		public int tyre_set;
		public int[] camber = new int[4] { 0, 0, 0, 0 };
		public int[] toe = new int[4] { 0, 0, 0, 0 };
		public int caster_lf;
		public int caster_rf;

		public Stint() { }

		public Stint(PluginManager pm, Values v, GameData data) {
			Update(pm, v, data);
		}

		public void Update(PluginManager pm, Values v, GameData data) {
			string stime = DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss");

			string sessType = data.NewData.SessionTypeName;
			if (sessType == "7") {
				sessType = "HOTSTINT";
			}
			session_type = sessType;
			stint_nr = v.laps.StintNr;
			start_time = stime;
			tyre_compound = v.car.Tyres.Name;

			for (var i = 0; i < 4; i++) {
				tyre_pres_in[i] = v.car.Tyres.CurrentInputPres[i];
			}

			if (RaceEngineerPlugin.GAME.IsACC) {
				brake_pad_front = (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.frontBrakeCompound") + 1;
				brake_pad_rear = (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.rearBrakeCompound") + 1;
				tyre_set = v.car.Tyres.currentTyreSet;
			} else {
				brake_pad_front = -1;
				brake_pad_rear = -1;
				tyre_set = -1;
			}

			brake_pad_nr = v.car.Brakes.SetNr;

			if (v.car.Setup != null) {
				brake_duct_front = v.car.Setup.advancedSetup.aeroBalance.brakeDuct[0];
				brake_duct_rear = v.car.Setup.advancedSetup.aeroBalance.brakeDuct[1];
				for (var i = 0; i < 4; i++) {
					camber[i] = v.car.Setup.basicSetup.alignment.camber[i];
					toe[i] = v.car.Setup.basicSetup.alignment.toe[i];
				}

				caster_lf = v.car.Setup.basicSetup.alignment.casterLF;
				caster_rf = v.car.Setup.basicSetup.alignment.casterRF;
			} else {
				brake_duct_front = -1;
				brake_duct_rear = -1;
				for (var i = 0; i < 4; i++) {
					camber[i] = -1;
					toe[i] = -1;
				}

				caster_lf = -1;
				caster_rf = -1;
			}

		}
	}

	public class Lap {
		public int session_lap_nr;
		public int stint_lap_nr;
		public int tyreset_lap_nr;
		public int brake_lap_nr;
		public double air_temp;
		public double air_temp_delta;
		public double track_temp;
		public double track_temp_delta;
		public double lap_time;
		public double fuel_used;
		public double fuel_left;

		public double[] tyre_pres_avg = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] tyre_pres_min = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] tyre_pres_max = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] tyre_pres_loss = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public bool[] tyre_pres_loss_lap = new bool[4] { false, false, false, false };

		public double[] tyre_temp_avg = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] tyre_temp_min = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] tyre_temp_max = new double[4] { 0.0, 0.0, 0.0, 0.0 };

		public double[] brake_temp_avg = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] brake_temp_min = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] brake_temp_max = new double[4] { 0.0, 0.0, 0.0, 0.0 };

		public double[] tyre_life_left = new double[4] { 0.0, 0.0, 0.0, 0.0 };

		public double[] pad_life_left = new double[4] { 0.0, 0.0, 0.0, 0.0 };
		public double[] disc_life_left = new double[4] { 0.0, 0.0, 0.0, 0.0 };

		public int abs;
		public int tc;
		public int tc2;
		public int ecu_map;
		public bool ecu_map_changed;
		public string track_grip_status;
		public bool is_valid;
		public bool is_valid_fuel_lap;
		public bool is_outlap;
		public bool is_inlap;

		public Lap() { }


		public Lap(PluginManager pm, Values v, GameData data) {
			Update(pm, v, data);
		}


		public void Update(PluginManager pm, Values v, GameData data) {
			session_lap_nr = data.NewData.CompletedLaps;
			stint_lap_nr = v.laps.StintLaps;
			if (RaceEngineerPlugin.GAME.IsACC) {
				tyreset_lap_nr = v.car.Tyres.GetCurrentSetLaps();
			} else {
				tyreset_lap_nr = 0;
			}
			brake_lap_nr = v.car.Brakes.LapsNr;
			air_temp = data.NewData.AirTemperature;
			air_temp_delta = data.NewData.AirTemperature - v.temps.AirAtLapStart;
			track_temp = data.NewData.RoadTemperature;
			track_temp_delta = data.NewData.RoadTemperature - v.temps.TrackAtLapStart;
			lap_time = v.laps.LastTime;
			fuel_used = v.car.Fuel.LastUsedPerLap;
			fuel_left = v.car.Fuel.Remaining;

			for (var i = 0; i < 4; i++) {
				tyre_pres_avg[i] = v.car.Tyres.PresOverLap[i].Avg;
				tyre_pres_min[i] = v.car.Tyres.PresOverLap[i].Min;
				tyre_pres_max[i] = v.car.Tyres.PresOverLap[i].Max;
				tyre_pres_loss[i] = v.car.Tyres.PresLoss[i];
				tyre_pres_loss_lap[i] = v.car.Tyres.PresLossLap[i];

				tyre_temp_avg[i] = v.car.Tyres.TempOverLap[i].Avg;
				tyre_temp_min[i] = v.car.Tyres.TempOverLap[i].Min;
				tyre_temp_max[i] = v.car.Tyres.TempOverLap[i].Max;

				brake_temp_avg[i] = v.car.Brakes.TempOverLap[i].Avg;
				brake_temp_min[i] = v.car.Brakes.TempOverLap[i].Min;
				brake_temp_max[i] = v.car.Brakes.TempOverLap[i].Max;
			}

			abs = data.NewData.ABSLevel;
			tc = data.NewData.TCLevel;
			ecu_map = data.NewData.EngineMap;
			ecu_map_changed = v.booleans.OldData.EcuMapChangedThisLap;

			if (RaceEngineerPlugin.GAME.IsACC) {
				tc2 = (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.TCCut");

				for (var i = 0; i < 4; i++) {
					pad_life_left[i] = (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife0" + $"{i + 1}");
					disc_life_left[i] = (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.discLife0" + $"{i + 1}");
				}

				track_grip_status = RaceEngineerPlugin.TrackGripStatus(pm);
			} else {
				tc2 = -1;
				for (var i = 0; i < 4; i++) {
					pad_life_left[i] = -1;
				}

				track_grip_status = null;
			}

			is_valid = v.booleans.NewData.SavePrevLap;
			// Need to use booleans.OldData which is the last point on finished lap
			is_valid_fuel_lap = v.booleans.OldData.IsValidFuelLap;
			is_outlap = v.booleans.OldData.IsOutLap;
			is_inlap = v.booleans.OldData.IsInLap;
		}
	}


	/// <summary>
	/// Handles data collection/storing for plugin.
	/// </summary>
	public class Database : IDisposable {
		private SQLiteConnection conn;

		private SQLiteCommand insertEventCmd;
		private SQLiteCommand insertStintCmd;
		private SQLiteCommand insertLapCmd;

		private SQLiteTransaction transaction;

		private long eventId;
		private long stintId;
		private int numCommands = 0;
		private Lap prevLap = new Lap();
		private Stint stint = new Stint();
		private Task lastTask;

		public Database() {
			var location = $@"{RaceEngineerPlugin.SETTINGS.DataLocation}\{RaceEngineerPlugin.GAME.Name}\data.db";
			if (!File.Exists(location)) {
				Directory.CreateDirectory(Path.GetDirectoryName(location));
			}
			conn = new SQLiteConnection($"Data Source={location}");
			try {
				conn.Open();
				eventsTable.CreateTable(conn);
				stintsTable.CreateTableWForeignKey(conn, $"FOREIGN KEY({EVENT_ID}) REFERENCES {eventsTable.name}({EVENT_ID})");
				lapsTable.CreateTableWForeignKey(conn, $"FOREIGN KEY({STINT_ID}) REFERENCES {stintsTable.name}({STINT_ID})");

				insertEventCmd = eventsTable.CreateInsertCmdWReturning(conn, EVENT_ID);
				insertStintCmd = stintsTable.CreateInsertCmdWReturning(conn, STINT_ID);
				insertLapCmd = lapsTable.CreateInsertCmd(conn);

				RaceEngineerPlugin.LogInfo($"Opened database from '{location}'");
			} catch (Exception ex) {
				RaceEngineerPlugin.LogInfo($"Failed to open DB. location={location} Error msq: {ex}");
			}
		}

		#region IDisposable Support
		~Database() { 
			Dispose(false);
		}
		
		private bool isDisposed = false;
		protected virtual void Dispose(bool disposing) {
			if (!isDisposed) {
				RaceEngineerPlugin.LogInfo("Disposed.");
				if (transaction != null) {
					transaction.Commit();
				}

				if (disposing) {
					insertEventCmd.Dispose();
					insertStintCmd.Dispose();
					insertLapCmd.Dispose();
				}

				if (transaction != null) {
					transaction.Dispose();
					transaction = null;
				}

				if (conn != null) {
					conn.Close();
					conn.Dispose();
					conn = null;
				}

				isDisposed = true;
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		public void CommitTransaction() {
			if (transaction != null && numCommands != 0) {
				//Stopwatch sw = Stopwatch.StartNew();
				if (lastTask != null) {
					lastTask.Wait();
					lastTask = null;
				}
				RaceEngineerPlugin.LogInfo("Commited db transaction");
				transaction.Commit();
				transaction.Dispose();
				transaction = null;
				numCommands = 0;
				//sw.Stop();
				//var ts = sw.Elapsed;
				//File.AppendAllText($"{RaceEngineerPlugin.SETTINGS.DataLocation}\\Logs\\RETiming_DBCommitTransaction_{RaceEngineerPlugin.pluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");
			}
		}

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

		private static string[] TYRES = new string[] { "lf", "rf", "lr", "rr" };
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
			new DBField(TRACK_GRIP_STATUS, "TEXT"),
			new DBField(IS_VALID, "INTEGER"),
			new DBField(IS_VALID_FUEL_LAP, "INTEGER"),
			new DBField(IS_OUTLAP, "INTEGER"),
			new DBField(IS_INLAP, "INTEGER"),
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
			if (transaction == null) {
				transaction = conn.BeginTransaction();
			}
			if (lastTask != null) {
				lastTask.Wait();
				lastTask = null;
			}

			string stime = DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss");

			SetParam(insertEventCmd, CAR_ID, carName);
			SetParam(insertEventCmd, TRACK_ID, trackName);
			SetParam(insertEventCmd, START_TIME, stime);

			eventId = (long)insertEventCmd.ExecuteScalar();
			numCommands++;

			var debugCmd = new SQLiteCommand(conn);
			debugCmd.CommandText = $"SELECT * FROM {eventsTable.name} ORDER BY rowid DESC LIMIT 1";
			var rdr = debugCmd.ExecuteReader();
			rdr.Read();

			var txt = $"Inserted event @ {stime}";
			for (var i = 0; i < rdr.FieldCount; i++) {
				txt += $"\n\t{rdr.GetName(i)} = {rdr.GetValue(i)}";
			}

			RaceEngineerPlugin.LogInfo(txt);
		}

		public void InsertStint(PluginManager pm, Values v, GameData data) {
			if (transaction == null) transaction = conn.BeginTransaction();
			if (lastTask != null) lastTask.Wait();

			stint.Update(pm, v, data);
			lastTask = Task.Run(() => InsertStint());
		}

		private void InsertStint() {
			if (transaction == null) {
				transaction = conn.BeginTransaction();
			}

			SetParam(insertStintCmd, EVENT_ID, eventId);
			SetParam(insertStintCmd, SESSION_TYPE, stint.session_type);
			SetParam(insertStintCmd, STINT_NR, stint.stint_nr);
			SetParam(insertStintCmd, START_TIME, stint.start_time);
			SetParam(insertStintCmd, TYRE_COMPOUND, stint.tyre_compound);

			for (var i = 0; i < 4; i++) {
				SetParam(insertStintCmd, TYRE_PRES_IN + $"_{TYRES[i]}", stint.tyre_pres_in[i]);
			}

			SetParam(insertStintCmd, BRAKE_PAD_FRONT, stint.brake_pad_front);
			SetParam(insertStintCmd, BRAKE_PAD_REAR, stint.brake_pad_rear);
			SetParam(insertStintCmd, TYRE_SET, stint.tyre_set);
			SetParam(insertStintCmd, BRAKE_PAD_NR, stint.brake_pad_nr);
			SetParam(insertStintCmd, BRAKE_DUCT_FRONT, stint.brake_duct_front);
			SetParam(insertStintCmd, BRAKE_DUCT_REAR, stint.brake_duct_rear);
			for (var i = 0; i < 4; i++) {
				SetParam(insertStintCmd, CAMBER + $"_{TYRES[i]}", stint.camber[i]);
				SetParam(insertStintCmd, TOE + $"_{TYRES[i]}", stint.toe[i]);
			}
			SetParam(insertStintCmd, CASTER + $"_{TYRES[0]}", stint.caster_lf);
			SetParam(insertStintCmd, CASTER + $"_{TYRES[1]}", stint.caster_rf);


			stintId = (long)insertStintCmd.ExecuteScalar();
			//insertStintCmd.Reset();
			numCommands++;

			// Debug
			var debugCmd = new SQLiteCommand(conn);
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

			RaceEngineerPlugin.LogInfo(txt);
		}

		public void InsertLap(PluginManager pm, Values v, GameData data) {
			if (transaction == null) transaction = conn.BeginTransaction();
			if (lastTask != null) lastTask.Wait();
			
			prevLap.Update(pm, v, data);
			lastTask = Task.Run(() => InsertLap());
		}

		private void InsertLap() {
			SetParam(insertLapCmd, STINT_ID, stintId);
			SetParam(insertLapCmd, SESSION_LAP_NR, prevLap.session_lap_nr);
			SetParam(insertLapCmd, STINT_LAP_NR, prevLap.stint_lap_nr);
			SetParam(insertLapCmd, TYRESET_LAP_NR, prevLap.tyreset_lap_nr);
			SetParam(insertLapCmd, BRAKE_PAD_LAP_NR, prevLap.brake_lap_nr);
			SetParam(insertLapCmd, AIR_TEMP, prevLap.air_temp);
			SetParam(insertLapCmd, AIR_TEMP_DELTA, prevLap.air_temp_delta);
			SetParam(insertLapCmd, TRACK_TEMP, prevLap.track_temp);
			SetParam(insertLapCmd, TRACK_TEMP_DELTA, prevLap.track_temp_delta);
			SetParam(insertLapCmd, LAP_TIME, prevLap.lap_time);
			SetParam(insertLapCmd, FUEL_USED, prevLap.fuel_used);
			SetParam(insertLapCmd, FUEL_LEFT, prevLap.fuel_left);

			for (var i = 0; i < 4; i++) {
				var tyre = $"_{TYRES[i]}";
				SetParam(insertLapCmd, TYRE_PRES_AVG + tyre, prevLap.tyre_pres_avg[i]);
				SetParam(insertLapCmd, TYRE_PRES_MIN + tyre, prevLap.tyre_pres_min[i]);
				SetParam(insertLapCmd, TYRE_PRES_MAX + tyre, prevLap.tyre_pres_max[i]);
				SetParam(insertLapCmd, TYRE_PRES_LOSS + tyre, prevLap.tyre_pres_loss[i]);
				SetParam(insertLapCmd, TYRE_PRES_LOSS_LAP + tyre, prevLap.tyre_pres_loss_lap[i]);

				SetParam(insertLapCmd, TYRE_TEMP_AVG + tyre, prevLap.tyre_temp_avg[i]);
				SetParam(insertLapCmd, TYRE_TEMP_MIN + tyre, prevLap.tyre_temp_min[i]);
				SetParam(insertLapCmd, TYRE_TEMP_MAX + tyre, prevLap.tyre_temp_max[i]);

				SetParam(insertLapCmd, BRAKE_TEMP_AVG + tyre, prevLap.brake_temp_avg[i]);
				SetParam(insertLapCmd, BRAKE_TEMP_MIN + tyre, prevLap.brake_temp_min[i]);
				SetParam(insertLapCmd, BRAKE_TEMP_MAX + tyre, prevLap.brake_temp_max[i]);

				SetParam(insertLapCmd, TYRE_LIFE_LEFT + tyre, 0.0);
			}

			SetParam(insertLapCmd, ABS, prevLap.abs);
			SetParam(insertLapCmd, TC, prevLap.tc);
			SetParam(insertLapCmd, ECU_MAP, prevLap.ecu_map);
			SetParam(insertLapCmd, ECU_MAP_CHANGED, prevLap.ecu_map_changed);

			SetParam(insertLapCmd, TC2, prevLap.tc2);
			SetParam(insertLapCmd, TRACK_GRIP_STATUS, prevLap.track_grip_status);

			for (var i = 0; i < 4; i++) {
				SetParam(insertLapCmd, PAD_LIFE_LEFT + $"_{TYRES[i]}", prevLap.pad_life_left[i]);
				SetParam(insertLapCmd, DISC_LIFE_LEFT + $"_{TYRES[i]}", prevLap.disc_life_left[i]);
			}

			SetParam(insertLapCmd, IS_VALID, prevLap.is_valid);
			// Need to use booleans.OldData which is the last point on finished lap
			SetParam(insertLapCmd, IS_VALID_FUEL_LAP, prevLap.is_valid_fuel_lap);
			SetParam(insertLapCmd, IS_OUTLAP, prevLap.is_outlap);
			SetParam(insertLapCmd, IS_INLAP, prevLap.is_inlap);


			insertLapCmd.ExecuteNonQuery();
			numCommands++;


            // Debug log inserted values
            var debugCmd = new SQLiteCommand(conn);
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
                if (cname.EndsWith("lf")) {
                    txt += $"\n\t{cname} = [{fmt(i)}, {fmt(i + 1)}, {fmt(i + 2)}, {fmt(i + 3)}]";
                    i += 3;
                } else {
                    txt += $"\n\t{cname} = {fmt(i)}";
                }
            }

            RaceEngineerPlugin.LogInfo(txt);

        }
		#endregion

		#region QUERIES

		public List<PrevData> GetPrevSessionData(string carName, string trackName, int numItems, int trackGrip) {
			CommitTransaction();
			string conds = $"AND l.{IS_VALID} AND l.{TRACK_GRIP_STATUS} IN ";
			if (0 < trackGrip && trackGrip < 3) {
				conds += "('Green', 'Fast', 'Optimum')";
			} else if (trackGrip == 3) {
				conds += "('Green', 'Fast', 'Optimum', 'Greasy', 'Damp')";
			} else if (trackGrip == 4) {
				conds += "('Greasy', 'Damp', 'Wet')";
			} else {
				conds += "('Damp', 'Wet', 'Flooded')";
			}

			var cmd = new SQLiteCommand(conn);
			cmd.CommandText = $@"SELECT l.{LAP_TIME}, l.{FUEL_USED} FROM {lapsTable.name} AS l 
				INNER JOIN {stintsTable.name} AS s ON l.{STINT_ID} == s.{STINT_ID} 
				INNER JOIN {eventsTable.name} AS e ON e.{EVENT_ID} == s.{EVENT_ID} 
				WHERE e.{CAR_ID} == '{carName}' AND e.{TRACK_ID} == '{trackName}' {conds}
				ORDER BY l.{LAP_ID} DESC
				LIMIT {numItems}";

			SQLiteDataReader rdr = cmd.ExecuteReader();
			List<PrevData> list = new List<PrevData>(numItems);

			while (rdr.Read()) {
				if (HasNullFields(rdr)) continue;
				list.Add(new PrevData(rdr.GetDouble(0), rdr.GetDouble(1)));
			}

			return list;
		}

		private const int LAP_NR_LOW_THRESHOLD = 2;
		private const int LAP_NR_HIGH_THRESHOLD = 11;
		private const double TYRE_PRES_LOSS_THRESHOLD = 0.25;
		private const double AIR_TEMP_CHANGE_THRESHOLD = 0.25;
		private const double TRACK_TEMP_CHANGE_THRESHOLD = 0.25;
		public Tuple<List<double[]>, List<double>> GetInputPresData(int tyre, string car, string track, int brakeDuct, string compound, string track_grip_status) {
			CommitTransaction();
			var cmd = new SQLiteCommand(conn);
			string duct;
			if (tyre < 2) {
				duct = BRAKE_DUCT_FRONT;
			} else {
				duct = BRAKE_DUCT_REAR;
			}

			string track_grip;
			if (track_grip_status == "Green" || track_grip_status == "Fast" || track_grip_status == "Optimum") {
				// For dry conditions use all available dry data, pressures don't change that much
				// Potentially could learn difference and apply to data.
				track_grip = "'Green', 'Fast', 'Optimum'";
			} else {
				// For wet use only given conditions. Even then it's not that accurate since WET includes light rain, medium rain and so on.
				track_grip = $"'{track_grip_status}'";
			}

			var ty = TYRES[tyre];
			cmd.CommandText = $@"
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
					AND l.{TYRE_PRES_LOSS_LAP}_{ty} == 0";
			if (-1 < brakeDuct && brakeDuct < 7) {
				cmd.CommandText += $" AND s.{duct} == {brakeDuct}";
			}
			if (track_grip != "Unknown") {
				cmd.CommandText += $" AND l.{TRACK_GRIP_STATUS} in ({track_grip})";
			}

			SQLiteDataReader rdr = cmd.ExecuteReader();
			List<double> y = new List<double>();
			List<double[]> x = new List<double[]>();
			while (rdr.Read()) {
				if (HasNullFields(rdr)) continue;

				y.Add(rdr.GetDouble(0));
				// Homogeneous coordinate, avg_press - loss, air_temp, track_temp
				x.Add(new double[] { 1.0, rdr.GetDouble(1) - rdr.GetDouble(2), rdr.GetDouble(3), rdr.GetDouble(4) });
			}

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