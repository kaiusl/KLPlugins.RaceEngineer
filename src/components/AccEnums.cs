using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaceEngineerPlugin.ACCEnums {
    public enum Flag { 
        NoFlag = 0,
        Blue,
        Yellow,
        Black,
        White,
        Checkered,
        Penalty,
        Green,
        Orange
    }

    public enum Session { 
        Unknown = -1,
        Practice = 0,
        Qualify = 1,
        Race,
        Hotlap,
        Timeattack,
        Drift,
        Drag,
        Hotstint,
        Superpole
    }

    public enum GameStatus { 
        Off = 0,
        Replay,
        Live,
        Pause
    }

    public enum TrackGrip {
        Green = 0,
        Fast,
        Optimum,
        Greasy,
        Damp,
        Wet,
        Flooded
    }

    public enum RainIntensity { 
        NoRain = 0,
        Drizzle,
        Light,
        Medium,
        Heavy,
        Thunderstorm
    }

    public enum Tyre { 
        Dry,
        Wet
    }


}
