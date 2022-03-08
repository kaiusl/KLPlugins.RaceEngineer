using MathNet.Numerics.Statistics;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RaceEngineerPlugin.Stats {
    /// <summary>
    /// Base class to build different statistics implementations
    /// </summary>
    public class Stats {
        public static readonly string[] names = new string[SIZE] { "Min", "Max", "Avg", "Std", "Median", "Q1", "Q3" };
        public double[] Data { get; }
        public double Min { get => Data[0]; set => Data[0] = value; }
        public double Max { get => Data[1]; set => Data[1] = value; }
        public double Avg { get => Data[2]; set => Data[2] = value; }
        public double Std { get => Data[3]; set => Data[3] = value; }
        public double Median { get => Data[4]; set => Data[4] = value; }
        public double Q1 { get => Data[5]; set => Data[5] = value; }
        public double Q3 { get => Data[6]; set => Data[6] = value; }

        private const int SIZE = 7;

        public Stats() {
            Data = new double[SIZE] { double.NegativeInfinity, double.PositiveInfinity, double.NaN, 0.0, double.NaN, double.NegativeInfinity, double.PositiveInfinity };
        }

        public Stats(Stats o) {
            o.Data.CopyTo(Data, 0);
        }

        public Stats(RunningStatistics o) {
            Set(o);
        }

        public void Reset() {
            for (int i = 0; i < SIZE; i++) {
                Data[i] = double.NaN;
            }
        }

        public void Set(double value) {
            Min = value;
            Max = value;
            Avg = value;
            Std = 0.0;
            Median = value;
            Q1 = double.NegativeInfinity;
            Q3 = double.PositiveInfinity;
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
        public RunningStatistics[] Data { get; }

        public RunningStatistics LF { get => Data[0]; }
        public RunningStatistics RF { get => Data[1]; }
        public RunningStatistics LR { get => Data[2]; }
        public RunningStatistics RR { get => Data[3]; }


        public WheelsRunningStats() {
            Data = new RunningStatistics[] { new RunningStatistics(), new RunningStatistics(), new RunningStatistics(), new RunningStatistics() };
        }

        public void Reset() {
            for (int i = 0; i < SIZE; i++) {
                Data[i] = new RunningStatistics();
            }
        }

        public void Update(double[] values) {
            for (int i = 0; i < SIZE; i++) {
                Data[i].Push(values[i]);
            }
        }

        public RunningStatistics this[int key] {
            get => Data[key];
        }

    }

}