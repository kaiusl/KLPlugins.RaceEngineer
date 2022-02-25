using System;

namespace RaceEngineerPlugin {
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class RaceEngineerPluginSettings
    {
        public int SpeedWarningLevel = 100;
    }


    // Example Settings.json
    //{
    //"NumPreviousValuesStored": 15,
    //"DataLocation": "PluginsData\\ExtraInfoPlugin",
    //"TempColor": ["#87cefa", "#00ff7f", "#00ff7f", "#e60000"],
    //"TyreTempColorDefValues": [70.0, 80.0, 90.0, 100.0],
    //"BrakeTempColorDefValues": [200.0, 300.0, 500.0, 700.0],
    //"PresColor": ["#87cefa", "#00ff7f", "#00ff7f", "#e60000"],
    //"TyrePresColorDefValues": [26.5, 27.25, 27.75, 28.5],
    //"TimeColor": ["#00ff7f", "#F8F8FF", "#e60000"],
    //"TimeGraphColor": ["#00ff7f", "#F8F8FF", "#e60000"],
    //"TimeColorDeltaValues": [-1.0, 0.0, 1.0],
    //"FuelGraphColor": ["#00ff7f", "#F8F8FF", "#e60000"],
    //"FuelGraphColorValues": [-1.0, 0.0, 1.0]
    //}

    public class Settings {
        public int NumPreviousValuesStored { get; set; }
        public string DataLocation { get; set; }
        public string AccDataLocation { get; set; }

        public string[] TempColor { get; set; }
        public double[] TyreTempColorDefValues { get; set; }
        public double[] BrakeTempColorDefValues { get; set; }
        public string[] PresColor { get; set; }
        public double[] TyrePresColorDefValues { get; set; }
        public string[] TimeColor { get; set; }
        public string[] TimeGraphColor { get; set; }

        public double[] TimeColorDeltaValues { get; set; }
        public string[] FuelGraphColor { get; set; }
        public double[] FuelGraphColorValues { get; set; }
        public bool Log { get; set; }

        public Settings() {
            NumPreviousValuesStored = 15;
            DataLocation = "PluginsData\\RaceEngineerPlugin";
            AccDataLocation = "C:\\Users\\" + Environment.UserName + "\\Documents\\Assetto Corsa Competizione";
            TempColor = new string[] { "#87cefa", "#00ff7f", "#00ff7f", "#e60000" };
            TyreTempColorDefValues = new double[] { 70.0, 80.0, 90.0, 100.0 };
            BrakeTempColorDefValues = new double[] { 200.0, 300.0, 500.0, 700.0 };
            PresColor = new string[] { "#87cefa", "#00ff7f", "#00ff7f", "#e60000" };
            TyrePresColorDefValues = new double[] { 26.5, 27.25, 27.75, 28.5 };
            TimeColor = new string[] { "#00ff7f", "#F8F8FF", "#e60000" };
            TimeGraphColor = new string[] { "#00ff7f", "#F8F8FF", "#e60000" };
            TimeColorDeltaValues = new double[] { -1.0, 0.0, 1.0 };
            FuelGraphColor = new string[] { "#00ff7f", "#F8F8FF", "#e60000" };
            FuelGraphColorValues = new double[] { -1.0, 0.0, 1.0 };
            Log = false;
        }
    }
}