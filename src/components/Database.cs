using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using GameReaderCommon;
using SimHub.Plugins;

namespace RaceEngineerPlugin.Database
{
	/// <summary>
	/// Handles data collection/storing for plugin.
	/// </summary>
	public class Database {
		private const string TAG = "EXTRA INFO (Database): ";
		private SQLiteConnection conn;

		private SQLiteCommand insertEventCmd;
		private SQLiteCommand insertStintCmd;
		private SQLiteCommand insertLapCmd;

		private long eventId;
		private long stintId;

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
			} catch (Exception ex) {
				LogInfo($"Failed to open DB. location={location} Error msq: {ex}");
			}
		}

		public void Dispose() {
			conn.Close();
			conn.Dispose();
		}

		/////////////////////////////////
		// Tables

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
			new DBField(TYRE_COMPOUND, "INTEGER"),
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
		private const string TRACK_TEMP = "track_temp";
		private const string LAP_TIME = "lap_time";
		private const string FUEL_USED = "fuel_used";
		private const string FUEL_LEFT = "fuel_left";
		private const string TYRE_PRES_AVG = "tyre_pres_avg";
		private const string TYRE_PRES_MIN = "tyre_pres_min";
		private const string TYRE_PRES_MAX = "tyre_pres_max";
		private const string TYRE_TEMP_AVG = "tyre_temp_avg";
		private const string TYRE_TEMP_MIN = "tyre_temp_min";
		private const string TYRE_TEMP_MAX = "tyre_temp_max";
		private const string BRAKE_TEMP_AVG = "brake_temp_avg";
		private const string BRAKE_TEMP_MIN = "brake_temp_min";
		private const string BRAKE_TEMP_MAX = "brake_temp_max";
		private const string TYRE_LIFE_LEFT = "tyre_life_left";
		private const string BRAKE_LIFE_LEFT = "brake_life_left";
		private const string ABS = "abs";
		private const string TC = "tc";
		private const string TC2 = "tc2";
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
			new DBField(TRACK_TEMP, "REAL"),
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

			new DBField(BRAKE_LIFE_LEFT + $"_{TYRES[0]}", "REAL"),
			new DBField(BRAKE_LIFE_LEFT + $"_{TYRES[1]}", "REAL"),
			new DBField(BRAKE_LIFE_LEFT + $"_{TYRES[2]}", "REAL"),
			new DBField(BRAKE_LIFE_LEFT + $"_{TYRES[3]}", "REAL"),
			new DBField(ABS, "INTEGER"),
			new DBField(TC, "INTEGER"),
			new DBField(TC2, "INTEGER"),
			new DBField(TRACK_GRIP_STATUS, "TEXT"),
			new DBField(IS_VALID, "INTEGER"),
			new DBField(IS_VALID_FUEL_LAP, "INTEGER"),
			new DBField(IS_OUTLAP, "INTEGER"),
			new DBField(IS_INLAP, "INTEGER"),
		});


		/////////////////////////////////
		// Inserts

		private void SetParam(SQLiteCommand cmd, string name, object value) {
			cmd.Parameters.AddWithValue($"@{name}", value);
		}

		private void SetParam(SQLiteCommand cmd, string name, bool value) {
			SetParam(cmd, name, value ? 1 : 0);
		}

		private void SetParam(SQLiteCommand cmd, string name) {
			SetParam(cmd, name, DBNull.Value);
		}

		public void InsertEvent(string carName, string trackName) {
			string stime = DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss");
			LogInfo($"Inserted event: car={carName}, track={trackName} @ {stime}");

			SetParam(insertEventCmd, CAR_ID, carName);
			SetParam(insertEventCmd, TRACK_ID, trackName);
			SetParam(insertEventCmd, START_TIME, stime);

			insertEventCmd.Prepare();

			eventId = (long)insertEventCmd.ExecuteScalar();
			insertEventCmd.Reset();

			var debugCmd = new SQLiteCommand(conn);
			debugCmd.CommandText = $"SELECT * FROM {eventsTable.name} ORDER BY rowid DESC LIMIT 1";
			var rdr = debugCmd.ExecuteReader();
			rdr.Read();

			var txt = $"Inserted event @ {stime}";
			for (var i = 0; i < rdr.FieldCount; i++) {
				txt += $"\n\t{rdr.GetName(i)} = {rdr.GetValue(i)}";
			}

			LogInfo(txt);
		}

		public void InsertStint(PluginManager pm, Values v, GameData data) {
			string stime = DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss");

			SetParam(insertStintCmd, EVENT_ID, eventId);
			string sessType = data.NewData.SessionTypeName;
			if (sessType == "7") {
				sessType = "HOTSTINT";
			}
			SetParam(insertStintCmd, SESSION_TYPE, sessType);
			SetParam(insertStintCmd, STINT_NR, v.laps.StintNr);
			SetParam(insertStintCmd, START_TIME, stime);
			SetParam(insertStintCmd, TYRE_COMPOUND, v.car.Tyres.Name);

			for (var i = 0; i < 4; i++) {
				SetParam(insertStintCmd, TYRE_PRES_IN + $"_{TYRES[i]}", v.car.Tyres.CurrentInputPres[i]);
			}
			

			if (RaceEngineerPlugin.GAME.IsACC) {
				int tyreset = (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.currentTyreSet");
				SetParam(insertStintCmd, BRAKE_PAD_FRONT, (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.frontBrakeCompound") + 1);
				SetParam(insertStintCmd, BRAKE_PAD_REAR, (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.rearBrakeCompound") + 1);
				SetParam(insertStintCmd, TYRE_SET, tyreset);
			} else {
				SetParam(insertStintCmd, BRAKE_PAD_FRONT);
				SetParam(insertStintCmd, BRAKE_PAD_REAR);
				SetParam(insertStintCmd, TYRE_SET);
			}

			SetParam(insertStintCmd, BRAKE_PAD_NR, v.car.Brakes.PadNr);


			if (v.car.Setup == null) {
				SetParam(insertStintCmd, BRAKE_DUCT_FRONT);
				SetParam(insertStintCmd, BRAKE_DUCT_REAR);
				for (var i = 0; i < 4; i++) {
					SetParam(insertStintCmd, CAMBER + $"_{TYRES[i]}");
					SetParam(insertStintCmd, TOE + $"_{TYRES[i]}");
					if (i < 2) {
						SetParam(insertStintCmd, CASTER + $"_{TYRES[i]}");
					}
				}
			} else {
				SetParam(insertStintCmd, BRAKE_DUCT_FRONT, v.car.Setup.advancedSetup.aeroBalance.brakeDuct[0]);
				SetParam(insertStintCmd, BRAKE_DUCT_REAR, v.car.Setup.advancedSetup.aeroBalance.brakeDuct[1]);

				for (var i = 0; i < 4; i++) {
					SetParam(insertStintCmd, CAMBER + $"_{TYRES[i]}", v.car.Setup.basicSetup.alignment.camber[i]);
					SetParam(insertStintCmd, TOE + $"_{TYRES[i]}", v.car.Setup.basicSetup.alignment.toe[i]);
				}

				SetParam(insertStintCmd, CASTER + $"_{TYRES[0]}", v.car.Setup.basicSetup.alignment.casterLF);
				SetParam(insertStintCmd, CASTER + $"_{TYRES[1]}", v.car.Setup.basicSetup.alignment.casterRF);
			}

			insertStintCmd.Prepare();

			stintId = (long)insertStintCmd.ExecuteScalar();
			insertStintCmd.Reset();

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

			LogInfo(txt);
		}

		public void InsertLap(PluginManager pm, Values v, GameData data) {
			SetParam(insertLapCmd, STINT_ID, stintId);
			SetParam(insertLapCmd, SESSION_LAP_NR, data.NewData.CompletedLaps);
			SetParam(insertLapCmd, STINT_LAP_NR, v.laps.StintLaps);
			SetParam(insertLapCmd, TYRESET_LAP_NR, v.car.Tyres.SetLaps);
			SetParam(insertLapCmd, BRAKE_PAD_LAP_NR, v.car.Brakes.PadLaps);
			SetParam(insertLapCmd, AIR_TEMP, data.NewData.AirTemperature);
			SetParam(insertLapCmd, TRACK_TEMP, data.NewData.RoadTemperature);
			SetParam(insertLapCmd, LAP_TIME, v.laps.LastTime);
			SetParam(insertLapCmd, FUEL_USED, v.car.Fuel.LastUsedPerLap);
			SetParam(insertLapCmd, FUEL_LEFT, v.car.Fuel.Remaining);

			for (var i = 0; i < 4; i++) {
				var tyre = $"_{TYRES[i]}";
				SetParam(insertLapCmd, TYRE_PRES_AVG + tyre, v.car.Tyres.PresOverLap[i].Avg);
				SetParam(insertLapCmd, TYRE_PRES_MIN + tyre, v.car.Tyres.PresOverLap[i].Min);
				SetParam(insertLapCmd, TYRE_PRES_MAX + tyre, v.car.Tyres.PresOverLap[i].Max);

				SetParam(insertLapCmd, TYRE_TEMP_AVG + tyre, v.car.Tyres.TempOverLap[i].Avg);
				SetParam(insertLapCmd, TYRE_TEMP_MIN + tyre, v.car.Tyres.TempOverLap[i].Min);
				SetParam(insertLapCmd, TYRE_TEMP_MAX + tyre, v.car.Tyres.TempOverLap[i].Max);

				SetParam(insertLapCmd, BRAKE_TEMP_AVG + tyre, v.car.Brakes.TempOverLap[i].Avg);
				SetParam(insertLapCmd, BRAKE_TEMP_MIN + tyre, v.car.Brakes.TempOverLap[i].Min);
				SetParam(insertLapCmd, BRAKE_TEMP_MAX + tyre, v.car.Brakes.TempOverLap[i].Max);

				SetParam(insertLapCmd, TYRE_LIFE_LEFT + tyre);
			}
			
			SetParam(insertLapCmd, ABS, data.NewData.ABSLevel);
			SetParam(insertLapCmd, TC, data.NewData.TCLevel);

			if (RaceEngineerPlugin.GAME.IsACC) {
				SetParam(insertLapCmd, TC2, (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.TCCut"));

				var gs = (int)pm.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.trackGripStatus");
				string gs_str;
				switch (gs) { 
					case 0: gs_str = "Green"; break;
					case 1: gs_str = "Fast"; break;
					case 2: gs_str = "Optimum"; break;
					case 3: gs_str = "Greasy"; break;
					case 4: gs_str = "Damp"; break;
					case 5: gs_str = "Wet"; break;
					case 6: gs_str = "Flooded"; break;
					default: gs_str = "Unknown"; break;
				}

				for (var i = 0; i < 4; i++) {
					SetParam(insertLapCmd, BRAKE_LIFE_LEFT + $"_{TYRES[i]}", (float)pm.GetPropertyValue("DataCorePlugin.GameRawData.Physics.padLife0" + $"{i+1}"));
				}

				SetParam(insertLapCmd, TRACK_GRIP_STATUS, gs_str);
			} else {
				SetParam(insertLapCmd, TC2);
				SetParam(insertLapCmd, TRACK_GRIP_STATUS);

				for (var i = 0; i < 4; i++) {
					SetParam(insertLapCmd, BRAKE_LIFE_LEFT + $"_{TYRES[i]}");
				}
			}

			// Need to use booleans.OldData which is the last point on finished lap
			SetParam(insertLapCmd, IS_VALID, v.booleans.OldData.SaveLap);
			SetParam(insertLapCmd, IS_VALID_FUEL_LAP, v.booleans.OldData.IsValidFuelLap);
			SetParam(insertLapCmd, IS_OUTLAP, v.booleans.OldData.IsOutLap);
			SetParam(insertLapCmd, IS_INLAP, v.booleans.OldData.IsInLap);

			insertLapCmd.Prepare();

			insertLapCmd.ExecuteScalar();
			insertLapCmd.Reset();

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

			LogInfo(txt);

		}


		public List<PrevData> GetPrevSessionData(string carName, string trackName, int numItems, int trackGrip) {
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
				list.Add(new PrevData(rdr.GetDouble(0), rdr.GetDouble(1)));
			}

			return list;
		}

		public int GetLapsOnTyreset(int tyreSet) { 
			var cmd = new SQLiteCommand(conn);
			cmd.CommandText = $@"
				SELECT l.{TYRESET_LAP_NR} FROM {lapsTable.name} AS l
				INNER JOIN {stintsTable.name} AS s ON l.{STINT_ID} == s.{STINT_ID} 
				INNER JOIN {eventsTable.name} AS e ON e.{EVENT_ID} == s.{EVENT_ID} 
				WHERE e.{EVENT_ID} == {eventId} AND s.{TYRE_SET} == {tyreSet}
				ORDER BY l.{LAP_ID} DESC
				LIMIT 1";

			var result = cmd.ExecuteScalar();
			if (result != null) {
				var tmp = (long)result; // This cannot reasonably be out of int range
				LogInfo($"Fitted tyre set {tyreSet} is used for {tmp} laps.");
				return (int)tmp;
			} else {
				LogInfo($"Fitted tyre set {tyreSet} is new.");
				return 0;
			}
		}

		private void LogInfo(string msq) {
			SimHub.Logging.Current.Info(TAG + msq);
		}
	}

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