using MathNet.Numerics.Statistics;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace KLPlugins.RaceEngineer.Stats {
    /// <summary>
    /// Base class to build different statistics implementations
    /// </summary>
    public class Stats {
        public static readonly string[] Names = new string[_size] { "Min", "Max", "Avg", "Std", "Median", "Q1", "Q3" };
        public double[] Data { get; }
        public double Min { get => Data[0]; set => Data[0] = value; }
        public double Max { get => Data[1]; set => Data[1] = value; }
        public double Avg { get => Data[2]; set => Data[2] = value; }
        public double Std { get => Data[3]; set => Data[3] = value; }
        public double Median { get => Data[4]; set => Data[4] = value; }
        public double Q1 { get => Data[5]; set => Data[5] = value; }
        public double Q3 { get => Data[6]; set => Data[6] = value; }

        private const int _size = 7;

        public Stats() {
            Data = new double[_size] { double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN };
        }

        public Stats(Stats o) {
            o.Data.CopyTo(Data, 0);
        }

        public Stats(RunningStatistics o) {
            Set(o);
        }

        public void Reset() {
            Data[0] = double.NaN;
            Data[1] = double.NaN;
            Data[2] = double.NaN;
            Data[3] = double.NaN;
            Data[4] = double.NaN;
            Data[5] = double.NaN;
            Data[6] = double.NaN;
        }

        public void Set(double value) {
            Min = value;
            Max = value;
            Avg = value;
            Std = 0.0;
            Median = value;
            Q1 = double.NaN;
            Q3 = double.NaN;
        }

        public void Set(RunningStatistics o) {
            Min = o.Minimum;
            Max = o.Maximum;
            Avg = o.Mean;
            Std = o.StandardDeviation;
        }

        public double this[int key] {
            get => Data[key];
        }

    }

 
    /// <summary>
    /// Convenience class to simplyfy handling statistics of all four wheels.
    /// </summary>
    public class WheelsStats {
        public Stats[] Data { get; }
        public Stats Fl { get => Data[0]; }
        public Stats Fr { get => Data[1]; }
        public Stats Rl { get => Data[2]; }
        public Stats Rr { get => Data[3]; }

        private const int _size = 4;

        public WheelsStats() {
            Data = new Stats[] { new Stats(), new Stats(), new Stats(), new Stats() };
        }

        public WheelsStats(WheelsRunningStats o) {
            Data = new Stats[_size];
            for (int i = 0; i < _size; i++) {
                Data[i] = new Stats(o.Data[i]);
            }
        }

        public void Reset() {
            for (int i = 0; i < _size; i++) {
                Data[i].Reset();
            }
        }

        public void Update(WheelsRunningStats o) {
            for (int i = 0; i < _size; i++) {
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
        public RunningStatistics[] Data { get; }
        public RunningStatistics Fl { get => Data[0]; }
        public RunningStatistics Fr { get => Data[1]; }
        public RunningStatistics Rl { get => Data[2]; }
        public RunningStatistics Rr { get => Data[3]; }

        private const int _size = 4;

        public WheelsRunningStats() {
            Data = new RunningStatistics[] { new RunningStatistics(), new RunningStatistics(), new RunningStatistics(), new RunningStatistics() };
        }

        public void Reset() {
            for (int i = 0; i < _size; i++) {
                Data[i] = new RunningStatistics();
            }
        }

        public void Update(double[] values) {
            for (int i = 0; i < _size; i++) {
                Data[i].Push(values[i]);
            }
        }

        public RunningStatistics this[int key] {
            get => Data[key];
        }

    }

}