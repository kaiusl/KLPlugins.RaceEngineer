using System;
using System.Collections.Immutable;

using KLPlugins.RaceEngineer.Car;

using MathNet.Numerics.Statistics;

using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;

namespace KLPlugins.RaceEngineer.Stats {
    interface IStats {
        public double Min { get; }
        public double Max { get; }
        public double Avg { get; }
        public double Std { get; }
        public double Median { get; }
        public double Q1 { get; }
        public double Q3 { get; }
    }

    /// <summary>
    /// Base class to build different statistics implementations
    /// </summary>
    internal class Stats : IStats {
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

        internal ReadonlyStatsView AsReadonlyView() {
            return new ReadonlyStatsView(this);
        }

    }

    public readonly struct ReadonlyStatsView : IStats {
        private readonly Stats _stats;
        public double Min => this._stats.Min;
        public double Max => this._stats.Max;
        public double Avg => this._stats.Avg;
        public double Std => this._stats.Std;
        public double Median => this._stats.Median;
        public double Q1 => this._stats.Q1;
        public double Q3 => this._stats.Q3;

        internal ReadonlyStatsView(Stats stats) {
            this._stats = stats;
        }
    }


    /// <summary>
    /// Convenience class to simplyfy handling statistics of all four wheels.
    /// </summary>
    internal class WheelsStats : IWheelsData<Stats> {
        public Stats FL { get; } = new();
        public Stats FR { get; } = new();
        public Stats RL { get; } = new();
        public Stats RR { get; } = new();

        private const int _size = 4;

        internal WheelsStats() { }

        internal WheelsStats(WheelsRunningStats o) {
            this.Update(o);
        }

        internal void Reset() {
            this.FL.Reset();
            this.FR.Reset();
            this.RL.Reset();
            this.RR.Reset();
        }

        internal void Update(WheelsRunningStats o) {
            this.FL.Set(o.FL);
            this.FR.Set(o.FR);
            this.RL.Set(o.RL);
            this.RR.Set(o.RR);
        }

        public Stats this[int key] => key switch {
            0 => this.FL,
            1 => this.FR,
            2 => this.RL,
            3 => this.RR,
            _ => throw new IndexOutOfRangeException()
        };

        internal ReadonlyWheelsStatsView AsReadonlyView() {
            return new ReadonlyWheelsStatsView(this);
        }
    }

    public readonly struct ReadonlyWheelsStatsView : IWheelsData<ReadonlyStatsView> {
        private readonly WheelsStats _stats;
        public ReadonlyStatsView FL => this._stats.FL.AsReadonlyView();
        public ReadonlyStatsView FR => this._stats.FR.AsReadonlyView();

        public ReadonlyStatsView RL => this._stats.RL.AsReadonlyView();

        public ReadonlyStatsView RR => this._stats.RR.AsReadonlyView();

        public ReadonlyStatsView this[int index] => this._stats[index].AsReadonlyView();

        internal ReadonlyWheelsStatsView(WheelsStats stats) {
            this._stats = stats;
        }
    }

    /// <summary>
    /// Convenience class to simplyfy handling running statistics of all four wheels.
    /// </summary>
    internal class WheelsRunningStats : IWheelsData<RunningStatistics> {
        public RunningStatistics FL { get; private set; } = new();
        public RunningStatistics FR { get; private set; } = new();
        public RunningStatistics RL { get; private set; } = new();
        public RunningStatistics RR { get; private set; } = new();

        private const int _SIZE = 4;

        internal WheelsRunningStats() { }

        internal void Reset() {
            this.FL = new();
            this.FR = new();
            this.RL = new();
            this.RR = new();
        }

        internal void Update(double[] values) {
            this.FL.Push(values[0]);
            this.FR.Push(values[1]);
            this.RL.Push(values[2]);
            this.RR.Push(values[3]);
        }

        public RunningStatistics this[int key] => key switch {
            0 => this.FL,
            1 => this.FR,
            2 => this.RL,
            3 => this.RR,
            _ => throw new IndexOutOfRangeException()
        };

    }

    public class MinMaxAvg<T>(T min, T max, T avg) {
        public T Min { get; internal set; } = min;
        public T Max { get; internal set; } = max;
        public T Avg { get; internal set; } = avg;
    }

}