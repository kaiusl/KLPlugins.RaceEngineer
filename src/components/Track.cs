using GameReaderCommon;
using SimHub.Plugins;

namespace RaceEngineerPlugin.Track {

    public class Track {
        private const string TAG = RaceEngineerPlugin.PLUGIN_NAME + " (Track): ";
        public string Name { get; private set; }

        public void Reset() {
            Name = null;
        }

        public void OnNewEvent(GameData data) { 
            Reset();
            CheckChange(data.NewData.TrackId);
        }

        public void OnRegularUpdate(GameData data) {
            CheckChange(data.NewData.TrackId);
        }

        public void CheckChange(string newTrackName) {
            if (newTrackName != null) {
                var hasChanged = Name != newTrackName;
                if (hasChanged) {
                    LogInfo($"Track changed from '{Name}' to '{newTrackName}'");
                    Name = newTrackName;
                }
            }
        }

        private void LogInfo(string msq) {
            if (RaceEngineerPlugin.SETTINGS.Log) {
                SimHub.Logging.Current.Info(TAG + msq);
            }
        }


    }

}