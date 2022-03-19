using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SHACCRawData = ACSharedMemory.ACC.Reader.ACCRawData;

namespace RaceEngineerPlugin.RawData {
    public class ACCRawData {
        public SHACCRawData OldData { get; private set; }
        public SHACCRawData NewData { get; private set; }

        public ACCRawData() { }

        public void Update(SHACCRawData newData) {
            OldData = NewData;
            NewData = newData;
        }
    }
}
