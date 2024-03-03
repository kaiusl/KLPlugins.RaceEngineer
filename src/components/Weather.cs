using System.Collections.Generic;

using ACSharedMemory.ACC.MMFModels;

using GameReaderCommon;

using ksBroadcastingNetwork;

namespace KLPlugins.RaceEngineer {
    public class Weather {
        public double AirTemp { get; private set; }
        public double TrackTemp { get; private set; }
        public double AirTempAtLapStart { get; private set; }
        public double TrackTempAtLapStart { get; private set; }

        public List<WeatherPoint> Forecast { get; }

        public string WeatherSummary = "";

        private bool _isInitialForecastAdded = false;
        private double? _initalForecastTime = null;
        public int _daysSinceStart = 0;
        public bool _add10MinChange = true;
        // Keep track of weather changes, predict exact time for weather change

        public Weather() {
            this.Forecast = [];
            this.Reset();
        }

        public void Reset() {
            this.AirTemp = double.NaN;
            this.TrackTemp = double.NaN;
            this.AirTempAtLapStart = double.NaN;
            this.TrackTempAtLapStart = double.NaN;
            this.Forecast.Clear();
            this._isInitialForecastAdded = false;
            this._initalForecastTime = null;
            this._daysSinceStart = 0;
            this.WeatherSummary = "";
        }

        #region On... METHODS

        public void OnLapFinishedAfterInsert(GameData data) {
            this.AirTempAtLapStart = data.NewData.AirTemperature;
            this.TrackTempAtLapStart = data.NewData.RoadTemperature;
        }

        public void OnRegularUpdate(GameData data, Values v) {
            this.UpdateTemps(data, v);
            this.UpdateForecast(data, v);
        }

        #endregion


        #region Private methods

        public void UpdateTemps(GameData data, Values v) {


            if (v.Booleans.NewData.EnteredMenu) {
                this.AirTempAtLapStart = double.NaN;
                this.TrackTempAtLapStart = double.NaN;
                RaceEngineerPlugin.LogInfo($"Reset temps at lap start to '{this.AirTempAtLapStart}' and '{this.TrackTempAtLapStart}'");
            }

            if (v.Booleans.NewData.IsMoving && double.IsNaN(this.AirTempAtLapStart)) {
                bool set_lap_start_temps = false;

                SessionType sessionType = SessionTypeMethods.FromSHGameData(data);


                switch (sessionType) {
                    case SessionType.Race:
                    case SessionType.Hotstint:
                        if (data.OldData.SessionTimeLeft != data.NewData.SessionTimeLeft) {
                            set_lap_start_temps = true;
                        }
                        break;
                    default:
                        set_lap_start_temps = true;
                        break;
                }

                if (set_lap_start_temps) {
                    this.AirTempAtLapStart = data.NewData.AirTemperature;
                    this.TrackTempAtLapStart = data.NewData.RoadTemperature;
                    RaceEngineerPlugin.LogInfo($"Reset temps at lap start to '{this.AirTempAtLapStart}' and '{this.TrackTempAtLapStart}'");
                }
            }

            if (RaceEngineerPlugin.Game.IsAcc && data.NewData.AirTemperature == 0.0) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();

                this.AirTemp = rawDataNew.Realtime?.AmbientTemp ?? 0.0;
                this.TrackTemp = rawDataNew.Realtime?.TrackTemp ?? 0.0;
            } else {
                this.AirTemp = data.NewData.AirTemperature;
                this.TrackTemp = data.NewData.RoadTemperature;
            }
        }

        private const double SEC_IN_DAY = 24 * 3600;
        public void UpdateForecast(GameData data, Values v) {

            if (RaceEngineerPlugin.Game.IsAcc) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();
                var rawDataOld = (ACSharedMemory.ACC.Reader.ACCRawData)data.OldData.GetRawDataObject();

                var nowTime = rawDataNew.Graphics.clock + SEC_IN_DAY * this._daysSinceStart;
                if (v.Session.TimeMultiplier < 1) return;
                if (!this._isInitialForecastAdded) {
                    var now = rawDataNew.Graphics.rainIntensity;
                    var in10 = rawDataNew.Graphics.rainIntensityIn10min;
                    var in30 = rawDataNew.Graphics.rainIntensityIn30min;

                    if (in10 != now) {
                        this.Forecast.Add(new WeatherPoint(in10, nowTime + 10 * 60));
                    }
                    if (in30 != in10) {
                        this.Forecast.Add(new WeatherPoint(in30, nowTime + 30 * 60));
                    }
                    this._isInitialForecastAdded = true;
                    this._initalForecastTime = nowTime;
                    return;
                }

                if (rawDataNew.Graphics.clock < rawDataOld.Graphics.clock && rawDataNew.Graphics.globalRed == 0) {
                    // Graphics.clock can jump back and forth is session is not running, that could add false days. Check that we are in session.
                    this._daysSinceStart += 1;
                }

                // We only need to add changes from rainIntensityIn10Min for the first 20/TimeMultiplier minutes. After that rainIntensityIn10min repeats rainIntensityIn30min with 20/TimeMultiplier minute delay.
                if (this._add10MinChange && this._initalForecastTime != null && nowTime > this._initalForecastTime + 20 * 60) {
                    this._add10MinChange = false;
                }

                var changed = false;
                if (this._add10MinChange && rawDataOld.Graphics.rainIntensityIn10min != rawDataNew.Graphics.rainIntensityIn10min) {
                    this.Forecast.Add(new WeatherPoint(rawDataNew.Graphics.rainIntensityIn10min, nowTime + 10 * 60));
                    changed = true;
                }

                if (rawDataOld.Graphics.rainIntensityIn30min != rawDataNew.Graphics.rainIntensityIn30min) {
                    this.Forecast.Add(new WeatherPoint(rawDataNew.Graphics.rainIntensityIn30min, nowTime + 30 * 60));
                    changed = true;
                }

                // Remove points which are past
                var lenprev = this.Forecast.Count;
                this.Forecast.RemoveAll((w) => w.StartTime < nowTime);
                if (changed || lenprev != this.Forecast.Count) {
                    this.Forecast.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                }

                this.WeatherSummary = "";
                foreach (var weatherPoint in this.Forecast) {
                    if (v.Session.TimeMultiplier != 0) { // Weather won't change if timemult == 0, no reason to update weatherSummary
                        var deltaFromNow = (weatherPoint.StartTime - nowTime) / 60.0 / v.Session.TimeMultiplier;
                        this.WeatherSummary += $"{deltaFromNow:0.0}min: {ToPrettyString(weatherPoint.RainIntensity)}, ";
                    }
                }
            }
        }

        private static string ToPrettyString(ACC_RAIN_INTENSITY rainIntensity) {
            return rainIntensity switch {
                ACC_RAIN_INTENSITY.ACC_NO_RAIN => "No rain",
                ACC_RAIN_INTENSITY.ACC_DRIZZLE => "Drizzle",
                ACC_RAIN_INTENSITY.ACC_LIGHT_RAIN => "Light rain",
                ACC_RAIN_INTENSITY.ACC_MEDIUM_RAIN => "Medium rain",
                ACC_RAIN_INTENSITY.ACC_HEAVY_RAIN => "Heavy rain",
                ACC_RAIN_INTENSITY.ACC_THUNDERSTORM => "Storm",
                _ => "Unknown",
            };
        }

        #endregion

    }

    public class WeatherPoint(ACC_RAIN_INTENSITY rainIntensity, double startTime) {
        public ACC_RAIN_INTENSITY RainIntensity { get; } = rainIntensity;
        public double StartTime { get; } = startTime;

        /// <summary>
        /// Time when the given weather starts. 
        /// 
        /// If timeMultiplier is not set yet (is -1), we will assume that it's 1.
        /// If timeMultiplier is proper we store result as timeMultiplier cannot change in session.
        /// </summary>
        /// <param name="timeMultiplier"></param>
        /// <returns></returns>
        //public DateTime StartTime(int timeMultiplier) {
        //    if (_startTime != null) return (DateTime)_startTime;

        //    if (timeMultiplier == -1) {
        //        return _addTime.AddMinutes(_timeDeltaMin);
        //    } else if (timeMultiplier == 0) {
        //        _startTime = DateTime.MaxValue;
        //    } else {
        //        _startTime = _addTime.AddMinutes(_timeDeltaMin / timeMultiplier);
        //    }
        //    return (DateTime)_startTime;
        //}
    }


}