using System;
using System.Collections.Generic;
using System.Linq;
using Nito.Collections;
using MathNet.Numerics.Statistics;
using System.Diagnostics;

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
        public int Capacity { get => Data.Capacity; }
        public int Count { get => Data.Count; }
        public Stats.Stats Stats { get; }
        public double Min { get => Stats.Min; }
        public double Max { get => Stats.Max; }
        public double Avg { get => Stats.Avg; }
        public double Std { get => Stats.Std; }
        public double Median { get => Stats.Median; }
        public double Q1 { get => Stats.Q1; }
        public double Q3 { get => Stats.Q3; }

        private double _lowerBound = double.NegativeInfinity;
        private double _upperBound = double.PositiveInfinity;
        private RemoveOutliers _removeOutliers;
        private DescriptiveStatistics _stats;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedSizeDequeStats"/> class.
        /// </summary>
        /// <param name="size">The size.</param>
        public FixedSizeDequeStats(int size, RemoveOutliers removeOutliers) {
            Data = new Deque<double>(size);
            Stats = new Stats.Stats();
            this._removeOutliers = removeOutliers;
        }

        public void Clear() {
            Data.Clear();
            Stats.Reset();
            _lowerBound = double.NegativeInfinity;
            _upperBound = double.PositiveInfinity;
        }


        /// <summary>
        /// Add value, remove last if over size
        /// </summary>
        /// <param name="value">The value.</param>
        public void AddToFront(double value) {
            if (Count == Capacity) {
                double oldData = Data.RemoveFromBack();
            }
            Data.AddToFront(value);
            var data = Data.Where(x => !double.IsNaN(x));
            SetBounds(data);
            if (data.Count() > 1) {
                switch (_removeOutliers) {
                    case RemoveOutliers.Upper:
                        _stats = new DescriptiveStatistics(data.Where(x => x < _upperBound));
                        break;
                    case RemoveOutliers.Lower:
                        _stats = new DescriptiveStatistics(data.Where(x => _lowerBound < x));
                        break;
                    case RemoveOutliers.Both:
                        _stats = new DescriptiveStatistics(data.Where(x => _lowerBound < x && x < _upperBound));
                        break;
                    case RemoveOutliers.QPlus1:
                        _stats = new DescriptiveStatistics(data.Where(x => _lowerBound - 1 < x && x < _upperBound + 1));
                        break;
                    default:
                        _stats = new DescriptiveStatistics(data);
                        break;
                }
            } else {
                _stats = new DescriptiveStatistics(data);
            }

            Stats.Avg = _stats.Mean;
            Stats.Std = _stats.StandardDeviation;
            Stats.Min = _stats.Minimum;
            Stats.Max = _stats.Maximum;

            if (RaceEngineerPlugin.Settings.Log) {
                string txt = "Data = [";
                foreach (var a in Data) {
                    txt += $"{a:0.000}, ";
                }
                RaceEngineerPlugin.LogInfo($@"{txt}],
    (Min, Q1, Median, Q3, Max) = ({Min}, {Q1}, {Median}, {Q3}, {Max}),
    (Avg, Std) = ({Avg}, {Std}),
    (lowerBound, upperBound) = ({_lowerBound}, {_upperBound})");
            }
        }

        private void SetBounds(IEnumerable<double> data) {
            if (data.Count() > 1) {
                var s = Statistics.FiveNumberSummary(data);

                Stats.Q1 = s[1];
                Stats.Median = s[2];
                Stats.Q3 = s[3];

                var iqr3 = 3*(Q3 - Q1);
                _lowerBound = Q1 - iqr3;
                _upperBound = Q3 + iqr3;
            }  
        }

        public void Fill(double value) {
            Data.Clear();
            for (int i = 0; i < Capacity; i++) {
                Data.AddToFront(value);
            }
        }

        public double this[int key] {
            get => Data[key];
        }
    }
}