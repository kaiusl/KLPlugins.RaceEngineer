using GameReaderCommon;
using SimHub.Plugins;
using System;
using RaceEngineerPlugin.Stats;

namespace RaceEngineerPlugin.Car {

    public class Tyres {
        private const string TAG = "RACE ENGINEER (Tyres): ";
        public string Name { get; private set; }
        public double[] IdealInputPres { get; }
        public double[] CurrentInputPres { get; }
        public WheelsStats PresOverLap { get; }
        public WheelsStats TempOverLap { get; }
        public Color.ColorCalculator PresColorF { get; private set; }
        public Color.ColorCalculator PresColorR { get; private set; }
        public Color.ColorCalculator TempColorF { get; private set; }
        public Color.ColorCalculator TempColorR { get; private set; }
        public int SetLaps { get; private set; }

        private WheelsRunningStats presRunning = new WheelsRunningStats();
        private WheelsRunningStats tempRunning = new WheelsRunningStats();
        private TyreInfo tyreInfo = null;

        private double lastSampleTimeSec = DateTime.Now.Second;

        public Tyres() {
            PresOverLap = new WheelsStats();
            TempOverLap = new WheelsStats();
            IdealInputPres = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN};
            CurrentInputPres = new double[4] { double.NaN, double.NaN, double.NaN, double.NaN };
            SetLaps = 0;
            ResetColors();
        }

        public static string GetTyreCompound(PluginManager pluginManager) {
            return (RaceEngineerPlugin.GAME.IsAC || RaceEngineerPlugin.GAME.IsACC) ? (string)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.TyreCompound") : null;
        }

        //public void Update(in PluginManager pm, in GameData data, in Car.Car car) {
        //    CheckCompoundChange(pm, car);
        //    CheckPresChange(data);

        //}

        public void OnNewStint(PluginManager pluginManager, Database.Database db) {
            if (RaceEngineerPlugin.GAME.IsACC) {
                int tyreset = (int)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Graphics.currentTyreSet");
                SetLaps = db.GetLapsOnTyreset(tyreset);
            } else {
                SetLaps = 0;
            }
        }

        public void OnLapFinished() { 
            SetLaps += 1;
            UpdateIdealInputPressures();

            PresOverLap.Update(presRunning);
            TempOverLap.Update(tempRunning);

            presRunning.Reset();
            tempRunning.Reset();
        }


        public void CheckCompoundChange(PluginManager pluginManager, Car car) {
            string newTyreName = GetTyreCompound(pluginManager);
            if (newTyreName != null && newTyreName != Name && car.Info != null && car.Info.Tyres != null) {
                if (car.Info != null && car.Info.Tyres != null) {
                    LogInfo($"Tyres changed from '{Name}' to '{newTyreName}'.");
                    tyreInfo = car.Info.Tyres?[newTyreName];
                    if (tyreInfo != null) {
                        LogInfo("tyreInfo set");
                        PresColorF.UpdateInterpolation(tyreInfo.IdealPres.F, tyreInfo.IdealPresRange.F);
                        PresColorR.UpdateInterpolation(tyreInfo.IdealPres.R, tyreInfo.IdealPresRange.R);
                        TempColorF.UpdateInterpolation(tyreInfo.IdealTemp.F, tyreInfo.IdealTempRange.F);
                        TempColorR.UpdateInterpolation(tyreInfo.IdealTemp.R, tyreInfo.IdealTempRange.R);
                    } else {
                        LogInfo($"Current CarInfo '{car.Info}' doesn't have specs for tyre '{newTyreName}'. Resetting to defaults.");
                        ResetColors();
                    }
                    ResetValues();
                    Name = newTyreName;
                } else {
                    LogInfo($"Current CarInfo '{car.Name}' doesn't have specs for tyre '{newTyreName}'. Resetting to defaults.");
                    ResetColors();
                    ResetValues();
                    Name = null;
                }
            }
        }

        public void CheckPresChange(GameData data) {
            if (data.NewData.SpeedKmh < 10) {
                var havePressuresChanged = data.NewData.TyrePressureFrontLeft != 0 && (
                    Math.Abs(data.OldData.TyrePressureFrontLeft - data.NewData.TyrePressureFrontLeft) > 0.1
                    || Math.Abs(data.OldData.TyrePressureFrontRight - data.NewData.TyrePressureFrontRight) > 0.1
                    || Math.Abs(data.OldData.TyrePressureRearLeft - data.NewData.TyrePressureRearLeft) > 0.1
                    || Math.Abs(data.OldData.TyrePressureRearRight - data.NewData.TyrePressureRearRight) > 0.1);

                if (havePressuresChanged) {
                    LogInfo(String.Format("Input tyre pressures updated."));
                    CurrentInputPres[0] = data.NewData.TyrePressureFrontLeft;
                    CurrentInputPres[1] = data.NewData.TyrePressureFrontRight;
                    CurrentInputPres[2] = data.NewData.TyrePressureRearLeft;
                    CurrentInputPres[3] = data.NewData.TyrePressureRearRight;
                }
            }
        }

        public void UpdateOverLapData(GameData data, Booleans.Booleans booleans) {
            double now = DateTime.Now.Second;

            // Add sample to counters
            if (booleans.NewData.IsMoving && booleans.NewData.IsOnTrack && lastSampleTimeSec != now) {
                double[] currentPres = new double[] {
                    data.NewData.TyrePressureFrontLeft,
                    data.NewData.TyrePressureFrontRight,
                    data.NewData.TyrePressureRearLeft,
                    data.NewData.TyrePressureRearRight
                };
                double[] currentTemp = new double[] {
                    data.NewData.TyreTemperatureFrontLeft,
                    data.NewData.TyreTemperatureFrontRight,
                    data.NewData.TyreTemperatureRearLeft,
                    data.NewData.TyreTemperatureRearRight
                };

                presRunning.Update(currentPres);
                tempRunning.Update(currentTemp);

                lastSampleTimeSec = now;
            }
        }

        public void UpdateIdealInputPressures() {
            if (tyreInfo != null) {
                for (int i = 0; i < 4; i++) {
                    IdealInputPres[i] = CurrentInputPres[i] + (tyreInfo.IdealPres[i] - PresOverLap[i].Avg);
                }
            } else {
                LogInfo($"Couldn't update ideal tyre pressures as 'tyreInfo == null'");
            }
        }

        private void ResetColors() {
            PresColorF = new Color.ColorCalculator(RaceEngineerPlugin.SETTINGS.PresColor, RaceEngineerPlugin.SETTINGS.TyrePresColorDefValues);
            PresColorR = new Color.ColorCalculator(RaceEngineerPlugin.SETTINGS.PresColor, RaceEngineerPlugin.SETTINGS.TyrePresColorDefValues);
            TempColorF = new Color.ColorCalculator(RaceEngineerPlugin.SETTINGS.TempColor, RaceEngineerPlugin.SETTINGS.TyreTempColorDefValues);
            TempColorR = new Color.ColorCalculator(RaceEngineerPlugin.SETTINGS.TempColor, RaceEngineerPlugin.SETTINGS.TyreTempColorDefValues);
        }

        private void ResetValues() {
            Name = null;
            for (var i = 0; i < 4; i++) { 
                IdealInputPres[i] = double.NaN;
                CurrentInputPres[i] = double.NaN;
            }
            PresOverLap.Reset();
            TempOverLap.Reset();
            presRunning.Reset();
            tempRunning.Reset();
            //tyreInfo = null;
        }

        private void LogInfo(string msq) {
            SimHub.Logging.Current.Info(TAG + msq);
        }


    }

}