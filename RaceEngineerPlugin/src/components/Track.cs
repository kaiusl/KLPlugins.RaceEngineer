using GameReaderCommon;

namespace KLPlugins.RaceEngineer.Track {

    public class Track {
        public string? Name { get; private set; }

        internal void Reset() {
            this.Name = null;
        }

        internal void OnNewEvent(GameData data) {
            this.CheckChange(data.NewData.TrackId);
        }

        internal void OnRegularUpdate(GameData data) {
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