using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using System;
using System.Collections.Generic;

namespace RaceEngineerPlugin.Stats {
    /// <summary>
    /// Base class to build different statistics implementations
    /// </summary>
    public abstract class StatsBase {
        private const int SIZE = 4;
        public static readonly string[] names = new string[SIZE] { "Min", "Max", "Avg", "Std" };
        public double[] Data { get; }

        public StatsBase() {
            Data = new double[SIZE] { double.NaN, double.NaN, double.NaN, double.NaN };
        }

        public StatsBase(StatsBase o) {
            o.Data.CopyTo(Data, 0);
        }

        public double Min { get => Data[0]; set => Data[0] = value; }
        public double Max { get => Data[1]; set => Data[1] = value; }
        public double Avg { get => Data[2]; set => Data[2] = value; }

        public double Std { get => Data[3]; set => Data[3] = value; }

        public void Reset() {
            for (int i = 0; i < SIZE; i++) {
                Data[i] = double.NaN;
            }
        }

        public double this[int key] {
            get => Data[key];
        }
    }

    /// <summary>
    /// Holder for statistics values. Values are simply set and must be calculated separately.
    /// </summary>
    public class Stats : StatsBase {

        public Stats() : base() { }

        public Stats(Stats o) : base(o) { }

        public Stats(RunningStats o) : base(o) { }

        public void Set(double value) {
            Min = value;
            Max = value;
            Avg = value;
            Std = 0.0;
        }

        public void Set(double min, double max, double avg, double std) {
            Min = min;
            Max = max;
            Avg = avg;
            Std = std;
        }

        public void Set(RunningStats o) {
            Min = o.Min;
            Max = o.Max;
            Avg = o.Avg;
            Std = o.Std;
        }
    }

    /// <summary>
    /// Implementation of running statistics. Statistics are updated directly and data points are not kept. 
    /// </summary>
    public class RunningStats : StatsBase {

        private int _numSamples = 0;
        private double avgOfSquares = 0;

        public int NumSamples { get => _numSamples; }

        public RunningStats() : base() { }

        public RunningStats(RunningStats o) : base(o) {
            _numSamples = o.NumSamples;
        }

        public new void Reset() {
            base.Reset();
            _numSamples = 0;
        }

        public void Update(double value) {
            if (value < Min || double.IsNaN(Min)) Min = value;
            if (value > Max || double.IsNaN(Max)) Max = value;
            if (double.IsNaN(Avg)) {
                Avg = value;
                avgOfSquares = value * value;
                Std = 0.0;
            } else {
                Avg = (Avg * NumSamples + value) / (NumSamples + 1);
                avgOfSquares = (avgOfSquares * NumSamples + value * value) / (NumSamples + 1);
                Std = Math.Sqrt(avgOfSquares - Avg * Avg);
            }

            _numSamples++;
        }
    }

 
    /// <summary>
    /// Convenience class to simplyfy handling statistics of all four wheels.
    /// </summary>
    public class WheelsStats {
        private const int SIZE = 4;
        public Stats[] Data { get; }

        public Stats LF { get => Data[0]; }
        public Stats RF { get => Data[1]; }
        public Stats LR { get => Data[2]; }
        public Stats RR { get => Data[3]; }


        public WheelsStats() {
            Data = new Stats[] { new Stats(), new Stats(), new Stats(), new Stats() };
        }

        public WheelsStats(WheelsRunningStats o) {
            Data = new Stats[SIZE];
            for (int i = 0; i < SIZE; i++) {
                Data[i] = new Stats(o.Data[i]);
            }
        }

        public void Reset() {
            for (int i = 0; i < SIZE; i++) {
                Data[i].Reset();
            }
        }

        public void Update(WheelsRunningStats o) {
            for (int i = 0; i < SIZE; i++) {
                Data[i].Set(o.Data[i]);
            }
        }

        public Stats this[int key] {
            get => Data[key];
        }

    }

    /// <summary>
    /// Convenience class to simplyfy handling running statistics of all four wheels.
    /// </summary>
    public class WheelsRunningStats {
        private const int SIZE = 4;
        public RunningStats[] Data { get; }

        public RunningStats LF { get => Data[0]; }
        public RunningStats RF { get => Data[1]; }
        public RunningStats LR { get => Data[2]; }
        public RunningStats RR { get => Data[3]; }


        public WheelsRunningStats() {
            Data = new RunningStats[] { new RunningStats(), new RunningStats(), new RunningStats(), new RunningStats() };
        }

        public void Reset() {
            for (int i = 0; i < SIZE; i++) {
                Data[i].Reset();
            }
        }

        public void Update(double[] values) {
            for (int i = 0; i < SIZE; i++) {
                Data[i].Update(values[i]);
            }
        }

        public RunningStats this[int key] {
            get => Data[key];
        }

    }

}