using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using MathNet.Numerics.Statistics;

using Nito.Collections;

namespace KLPlugins.RaceEngineer.Deque {

    public enum RemoveOutliers {
        None,
        Lower,
        Upper,
        Both,
        QPlus1 // Fuel specific, remove anything thats outside (Q1 - 1, Q3 + 1), that's really extreme change and shouldn't ever happen, 
    }

    /// <summary>
    /// Deque that never grows larger than given size with some added statistics.
    /// 
    /// In our case we need some data structure to store previous lap values (lap times, fuel) 
    /// in which we need to push to the front (so that newest values are first) and remove from the back (eg remove oldest values).
    /// We also only want to store certain number of values, hence the fixed size bit.
    /// 
    /// In addition we would want some statistics on data.
    /// </summary>
    public class FixedSizeDequeStats {
        public Deque<double> Data { get; }
        public int Capacity { get => this.Data.Capacity; }
        public int Count { get => this.Data.Count; }
        public Stats.Stats Stats { get; }
        public double Min { get => this.Stats.Min; }
        public double Max { get => this.Stats.Max; }
        public double Avg { get => this.Stats.Avg; }
        public double Std { get => this.Stats.Std; }
        public double Median { get => this.Stats.Median; }
        public double Q1 { get => this.Stats.Q1; }
        public double Q3 { get => this.Stats.Q3; }

        private double _lowerBound = double.NegativeInfinity;
        private double _upperBound = double.PositiveInfinity;
        private RemoveOutliers _removeOutliers;
        private DescriptiveStatistics _stats;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedSizeDequeStats"/> class.
        /// </summary>
        /// <param name="size">The size.</param>
        public FixedSizeDequeStats(int size, RemoveOutliers removeOutliers) {
            this.Data = new Deque<double>(size);
            this.Stats = new Stats.Stats();
            this._removeOutliers = removeOutliers;
        }

        public void Clear() {
            this.Data.Clear();
            this.Stats.Reset();
            this._lowerBound = double.NegativeInfinity;
            this._upperBound = double.PositiveInfinity;
        }


        /// <summary>
        /// Add value, remove last if over size
        /// </summary>
        /// <param name="value">The value.</param>
        public void AddToFront(double value) {
            if (this.Count == this.Capacity) {
                double oldData = this.Data.RemoveFromBack();
            }
            this.Data.AddToFront(value);
            var data = this.Data.Where(x => !double.IsNaN(x));
            this.SetBounds(data);
            if (data.Count() > 1) {
                switch (this._removeOutliers) {
                    case RemoveOutliers.Upper:
                        this._stats = new DescriptiveStatistics(data.Where(x => x < this._upperBound));
                        break;
                    case RemoveOutliers.Lower:
                        this._stats = new DescriptiveStatistics(data.Where(x => this._lowerBound < x));
                        break;
                    case RemoveOutliers.Both:
                        this._stats = new DescriptiveStatistics(data.Where(x => this._lowerBound < x && x < this._upperBound));
                        break;
                    case RemoveOutliers.QPlus1:
                        this._stats = new DescriptiveStatistics(data.Where(x => this._lowerBound - 1 < x && x < this._upperBound + 1));
                        break;
                    default:
                        this._stats = new DescriptiveStatistics(data);
                        break;
                }
            } else {
                this._stats = new DescriptiveStatistics(data);
            }

            this.Stats.Avg = this._stats.Mean;
            this.Stats.Std = this._stats.StandardDeviation;
            this.Stats.Min = this._stats.Minimum;
            this.Stats.Max = this._stats.Maximum;

            if (RaceEngineerPlugin.Settings.Log) {
                string txt = "Data = [";
                foreach (var a in this.Data) {
                    txt += $"{a:0.000}, ";
                }
                RaceEngineerPlugin.LogInfo($@"{txt}],
    (Min, Q1, Median, Q3, Max) = ({this.Min}, {this.Q1}, {this.Median}, {this.Q3}, {this.Max}),
    (Avg, Std) = ({this.Avg}, {this.Std}),
    (lowerBound, upperBound) = ({this._lowerBound}, {this._upperBound})");
            }
        }

        private void SetBounds(IEnumerable<double> data) {
            if (data.Count() > 1) {
                var s = Statistics.FiveNumberSummary(data);

                this.Stats.Q1 = s[1];
                this.Stats.Median = s[2];
                this.Stats.Q3 = s[3];

                var iqr3 = 3 * (this.Q3 - this.Q1);
                this._lowerBound = this.Q1 - iqr3;
                this._upperBound = this.Q3 + iqr3;
            }
        }

        public void Fill(double value) {
            this.Data.Clear();
            for (int i = 0; i < this.Capacity; i++) {
                this.Data.AddToFront(value);
            }
        }

        public double this[int key] {
            get => this.Data[key];
        }
    }
}