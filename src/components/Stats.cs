using System;
using System.Collections.Generic;
using System.Diagnostics;

using MathNet.Numerics.Statistics;

using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;

namespace KLPlugins.RaceEngineer.Stats {
    /// <summary>
    /// Base class to build different statistics implementations
    /// </summary>
    public class Stats {
        public static readonly string[] Names = new string[_size] { "Min", "Max", "Avg", "Std", "Median", "Q1", "Q3" };
        public double[] Data { get; }
        public double Min { get => this.Data[0]; set => this.Data[0] = value; }
        public double Max { get => this.Data[1]; set => this.Data[1] = value; }
        public double Avg { get => this.Data[2]; set => this.Data[2] = value; }
        public double Std { get => this.Data[3]; set => this.Data[3] = value; }
        public double Median { get => this.Data[4]; set => this.Data[4] = value; }
        public double Q1 { get => this.Data[5]; set => this.Data[5] = value; }
        public double Q3 { get => this.Data[6]; set => this.Data[6] = value; }

        private const int _size = 7;

        public Stats() {
            this.Data = new double[_size] { double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN };
        }

        public Stats(Stats o) {
            o.Data.CopyTo(this.Data, 0);
        }

        public Stats(RunningStatistics o) {
            this.Set(o);
        }

        public void Reset() {
            this.Data[0] = double.NaN;
            this.Data[1] = double.NaN;
            this.Data[2] = double.NaN;
            this.Data[3] = double.NaN;
            this.Data[4] = double.NaN;
            this.Data[5] = double.NaN;
            this.Data[6] = double.NaN;
        }

        public void Set(double value) {
            this.Min = value;
            this.Max = value;
            this.Avg = value;
            this.Std = 0.0;
            this.Median = value;
            this.Q1 = double.NaN;
            this.Q3 = double.NaN;
        }

        public void Set(RunningStatistics o) {
            this.Min = o.Minimum;
            this.Max = o.Maximum;
            this.Avg = o.Mean;
            this.Std = o.StandardDeviation;
        }

        public double this[int key] {
            get => this.Data[key];
        }

    }


    /// <summary>
    /// Convenience class to simplyfy handling statistics of all four wheels.
    /// </summary>
    public class WheelsStats {
        public Stats[] Data { get; }
        public Stats Fl { get => this.Data[0]; }
        public Stats Fr { get => this.Data[1]; }
        public Stats Rl { get => this.Data[2]; }
        public Stats Rr { get => this.Data[3]; }

        private const int _size = 4;

        public WheelsStats() {
            this.Data = new Stats[] { new Stats(), new Stats(), new Stats(), new Stats() };
        }

        public WheelsStats(WheelsRunningStats o) {
            this.Data = new Stats[_size];
            for (int i = 0; i < _size; i++) {
                this.Data[i] = new Stats(o.Data[i]);
            }
        }

        public void Reset() {
            for (int i = 0; i < _size; i++) {
                this.Data[i].Reset();
            }
        }

        public void Update(WheelsRunningStats o) {
            for (int i = 0; i < _size; i++) {
                this.Data[i].Set(o.Data[i]);
            }
        }

        public Stats this[int key] {
            get => this.Data[key];
        }

    }

    /// <summary>
    /// Convenience class to simplyfy handling running statistics of all four wheels.
    /// </summary>
    public class WheelsRunningStats {
        public RunningStatistics[] Data { get; }
        public RunningStatistics Fl { get => this.Data[0]; }
        public RunningStatistics Fr { get => this.Data[1]; }
        public RunningStatistics Rl { get => this.Data[2]; }
        public RunningStatistics Rr { get => this.Data[3]; }

        private const int _size = 4;

        public WheelsRunningStats() {
            this.Data = new RunningStatistics[] { new RunningStatistics(), new RunningStatistics(), new RunningStatistics(), new RunningStatistics() };
        }

        public void Reset() {
            for (int i = 0; i < _size; i++) {
                this.Data[i] = new RunningStatistics();
            }
        }

        public void Update(double[] values) {
            for (int i = 0; i < _size; i++) {
                this.Data[i].Push(values[i]);
            }
        }

        public RunningStatistics this[int key] {
            get => this.Data[key];
        }

    }

}