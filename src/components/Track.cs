using GameReaderCommon;
using SimHub.Plugins;

namespace RaceEngineerPlugin.Track {

    public class Track {
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

        private void CheckChange(string newTrackName) {
            if (newTrackName != null) {
                var hasChanged = Name != newTrackName;
                if (hasChanged) {
                    RaceEngineerPlugin.LogInfo($"Track changed from '{Name}' to '{newTrackName}'");
                    Name = newTrackName;
                }
            }
        }

    }

}