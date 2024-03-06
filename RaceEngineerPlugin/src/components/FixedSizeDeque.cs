using System.Collections.Generic;
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
    /// <remarks>
    /// Initializes a new instance of the <see cref="FixedSizeDequeStats"/> class.
    /// </remarks>
    /// <param name="size">The size.</param>
    public class FixedSizeDequeStats(int size, RemoveOutliers removeOutliers) {
        public int Capacity => this._data.Capacity;
        public int Count => this._data.Count;
        public double Min => this.Stats.Min;
        public double Max => this.Stats.Max;
        public double Avg => this.Stats.Avg;
        public double Std => this.Stats.Std;
        public double Median => this.Stats.Median;
        public double Q1 => this.Stats.Q1;
        public double Q3 => this.Stats.Q3;

        internal Stats.Stats Stats { get; } = new Stats.Stats();

        private Deque<double> _data { get; } = new Deque<double>(size);

        private double _lowerBound = double.NegativeInfinity;
        private double _upperBound = double.PositiveInfinity;
        private readonly RemoveOutliers _removeOutliers = removeOutliers;
        private DescriptiveStatistics? _descriptiveStats;

        internal void Clear() {
            this._data.Clear();
            this.Stats.Reset();
            this._lowerBound = double.NegativeInfinity;
            this._upperBound = double.PositiveInfinity;
        }


        /// <summary>
        /// Add value, remove last if over size
        /// </summary>
        /// <param name="value">The value.</param>
        internal void AddToFront(double value) {
            if (this.Count == this.Capacity) {
                double oldData = this._data.RemoveFromBack();
            }
            this._data.AddToFront(value);
            var data = this._data.Where(x => !double.IsNaN(x));
            this.SetBounds(data);
            if (data.Count() > 1) {
                this._descriptiveStats = this._removeOutliers switch {
                    RemoveOutliers.Upper => new(data.Where(x => x < this._upperBound)),
                    RemoveOutliers.Lower => new(data.Where(x => this._lowerBound < x)),
                    RemoveOutliers.Both => new(data.Where(x => this._lowerBound < x && x < this._upperBound)),
                    RemoveOutliers.QPlus1 => new(data.Where(x => this._lowerBound - 1 < x && x < this._upperBound + 1)),
                    _ => new(data),
                };
            } else {
                this._descriptiveStats = new(data);
            }

            this.Stats.Avg = this._descriptiveStats.Mean;
            this.Stats.Std = this._descriptiveStats.StandardDeviation;
            this.Stats.Min = this._descriptiveStats.Minimum;
            this.Stats.Max = this._descriptiveStats.Maximum;

            if (RaceEngineerPlugin.Settings.Log) {
                string txt = "Data = [";
                foreach (var a in this._data) {
                    txt += $"{a:0.000}, ";
                }
                //            RaceEngineerPlugin.LogInfo($@"{txt}],
                //(Min, Q1, Median, Q3, Max) = ({Min}, {Q1}, {Median}, {Q3}, {Max}),
                //(Avg, Std) = ({Avg}, {Std}),
                //(lowerBound, upperBound) = ({_lowerBound}, {_upperBound})");
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

        internal void Fill(double value) {
            this._data.Clear();
            for (int i = 0; i < this.Capacity; i++) {
                this._data.AddToFront(value);
            }
        }

        public double this[int key] => this._data[key];
    }
}