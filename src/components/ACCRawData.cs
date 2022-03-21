using ksBroadcastingNetwork.Structs;
using SHACCRawData = ACSharedMemory.ACC.Reader.ACCRawData;

namespace RaceEngineerPlugin.RawData {
    public class ACCRawData {
        public SHACCRawData OldData { get; private set; }
        public SHACCRawData NewData { get; private set; }

        public ACCRawData() {
            OldData = new SHACCRawData();
            NewData = new SHACCRawData();
        }

        public void UpdateSharedMem(SHACCRawData newData) {
            if (newData == null) return;
 
            OldData.Physics = NewData.Physics;
            OldData.Graphics = NewData.Graphics;
            OldData.StaticInfo = NewData.StaticInfo;
            //OldData.Cars = NewData.Cars;

            NewData.Physics = newData.Physics;
            NewData.Graphics = newData.Graphics;
            NewData.StaticInfo = newData.StaticInfo;
        }

        public void OnBroadcastRealtimeUpdate(string sender, RealtimeUpdate update) {
            if (NewData.Realtime == null) {
                OldData.Realtime = update;
                NewData.Realtime = update;
            } else {
                OldData.Realtime = NewData.Realtime;
                NewData.Realtime = update;
            }
        }

        public void Reset() {
            OldData = new SHACCRawData();
            NewData = new SHACCRawData();
        }
    }
}
