// Original from ACC Broadcasting SDK example (Assetto Corsa Competizione Dedicated Server\sdk\broadcasting)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksBroadcastingNetwork.Structs
{
    public class BroadcastingEvent
    {
        public BroadcastingCarEventType Type { get; internal set; }
        public string Msg { get; internal set; }
        public int TimeMs { get; internal set; }
        public int CarId { get; internal set; }
        public CarInfo CarData { get; internal set; }
    }
}
