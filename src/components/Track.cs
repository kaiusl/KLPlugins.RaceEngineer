using GameReaderCommon;

namespace KLPlugins.RaceEngineer.Track {

    public class Track {
        public string? Name { get; private set; }

        public void Reset() {
            this.Name = null;
        }

        public void OnNewEvent(GameData data) {
            this.CheckChange(data.NewData.TrackId);
        }

        public void OnRegularUpdate(GameData data) {
            this.CheckChange(data.NewData.TrackId);
        }

        private void CheckChange(string? newTrackName) {
            if (newTrackName == null) return;

            if (this.Name != newTrackName) {
                RaceEngineerPlugin.LogInfo($"Track changed from '{this.Name}' to '{newTrackName}'");
                this.Name = newTrackName;
            }

        }

    }

}