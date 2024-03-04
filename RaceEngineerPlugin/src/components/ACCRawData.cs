using ksBroadcastingNetwork.Structs;

using SHACCRawData = ACSharedMemory.ACC.Reader.ACCRawData;

namespace KLPlugins.RaceEngineer.RawData {
    public class ACCRawData {
        public SHACCRawData OldData { get; private set; }
        public SHACCRawData NewData { get; private set; }

        public ACCRawData() {
            this.OldData = new SHACCRawData();
            this.NewData = new SHACCRawData();
        }

        public void Update(SHACCRawData newData) {
            this.OldData = this.NewData;
            this.NewData = newData;

            //if (newData == null) return;

            //OldData.Physics = NewData.Physics;
            //OldData.Graphics = NewData.Graphics;
            //OldData.StaticInfo = NewData.StaticInfo;
            ////OldData.Cars = NewData.Cars;

            //NewData.Physics = newData.Physics;
            //NewData.Graphics = newData.Graphics;
            //NewData.StaticInfo = newData.StaticInfo;
        }

        //public void OnBroadcastRealtimeUpdate(string sender, RealtimeUpdate update) {
        //    if (NewData.Realtime == null) {
        //        OldData.Realtime = update;
        //        NewData.Realtime = update;
        //    } else {
        //        OldData.Realtime = NewData.Realtime;
        //        NewData.Realtime = update;
        //    }
        //}

        public void Reset() {
            this.OldData = new SHACCRawData();
            this.NewData = new SHACCRawData();
        }
    }
}