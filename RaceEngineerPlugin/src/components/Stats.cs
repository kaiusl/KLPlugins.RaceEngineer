using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

using MathNet.Numerics.Statistics;

using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;

namespace KLPlugins.RaceEngineer.Stats {
    /// <summary>
    /// Base class to build different statistics implementations
    /// </summary>
    public class Stats {
        public static readonly ImmutableArray<string> Names = ImmutableArray.Create("Min", "Max", "Avg", "Std", "Median", "Q1", "Q3");
        private double[] _data { get; }
        public double Min { get => this._data[0]; set => this._data[0] = value; }
        public double Max { get => this._data[1]; set => this._data[1] = value; }
        public double Avg { get => this._data[2]; set => this._data[2] = value; }
        public double Std { get => this._data[3]; set => this._data[3] = value; }
        public double Median { get => this._data[4]; set => this._data[4] = value; }
        public double Q1 { get => this._data[5]; set => this._data[5] = value; }
        public double Q3 { get => this._data[6]; set => this._data[6] = value; }

        internal Stats() {
            this._data = [double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN];
        }

        internal Stats(Stats o) : this() {
            o._data.CopyTo(this._data, 0);
        }

        internal Stats(RunningStatistics o) : this() {
            this.Set(o);
        }

        internal void Reset() {
            this._data[0] = double.NaN;
            this._data[1] = double.NaN;
            this._data[2] = double.NaN;
            this._data[3] = double.NaN;
            this._data[4] = double.NaN;
            this._data[5] = double.NaN;
            this._data[6] = double.NaN;
        }

        internal void Set(double value) {
            this.Min = value;
            this.Max = value;
            this.Avg = value;
            this.Std = 0.0;
            this.Median = value;
            this.Q1 = double.NaN;
            this.Q3 = double.NaN;
        }

        internal void Set(RunningStatistics o) {
            this.Min = o.Minimum;
            this.Max = o.Maximum;
            this.Avg = o.Mean;
            this.Std = o.StandardDeviation;
        }

        public double this[int key] => this._data[key];

    }


    /// <summary>
    /// Convenience class to simplyfy handling statistics of all four wheels.
    /// </summary>
    public class WheelsStats {
        private Stats[] _data { get; }
        public Stats FL => this._data[0];
        public Stats FR => this._data[1];
        public Stats RL => this._data[2];
        public Stats RR => this._data[3];

        private const int _size = 4;

        internal WheelsStats() {
            this._data = [new(), new(), new(), new()];
        }

        internal WheelsStats(WheelsRunningStats o) {
            this._data = new Stats[_size];
            for (int i = 0; i < _size; i++) {
                this._data[i] = new(o[i]);
            }
        }

        internal void Reset() {
            for (int i = 0; i < _size; i++) {
                this._data[i].Reset();
            }
        }

        internal void Update(WheelsRunningStats o) {
            for (int i = 0; i < _size; i++) {
                this._data[i].Set(o[i]);
            }
        }

        public Stats this[int key] => this._data[key];

    }

    /// <summary>
    /// Convenience class to simplyfy handling running statistics of all four wheels.
    /// </summary>
    public class WheelsRunningStats {
        private RunningStatistics[] _data { get; }
        public RunningStatistics Fl => this._data[0];
        public RunningStatistics FR => this._data[1];
        public RunningStatistics Rl => this._data[2];
        public RunningStatistics RR => this._data[3];

        private const int _SIZE = 4;

        internal WheelsRunningStats() {
            this._data = [new(), new(), new(), new()];
        }

        internal void Reset() {
            for (int i = 0; i < _SIZE; i++) {
                this._data[i] = new();
            }
        }

        internal void Update(double[] values) {
            for (int i = 0; i < _SIZE; i++) {
                this._data[i].Push(values[i]);
            }
        }

        internal RunningStatistics this[int key] => this._data[key];

    }

    public class MinMaxAvg<T>(T min, T max, T avg) {
        public T Min { get; internal set; } = min;
        public T Max { get; internal set; } = max;
        public T Avg { get; internal set; } = avg;
    }

}