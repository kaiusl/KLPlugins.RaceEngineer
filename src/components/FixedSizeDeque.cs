using System;
using System.Collections.Generic;
using System.Linq;
using Nito.Collections;
using MathNet.Numerics.Statistics;
using System.Diagnostics;

namespace RaceEngineerPlugin.Deque {

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

        private double lowerBound = double.NegativeInfinity;
        private double upperBound = double.PositiveInfinity;
        private RemoveOutliers removeOutliers;
        private DescriptiveStatistics s;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedSizeDequeStats"/> class.
        /// </summary>
        /// <param name="size">The size.</param>
        public FixedSizeDequeStats(int size, RemoveOutliers removeOutliers) {
            Data = new Deque<double>(size);
            Stats = new Stats.Stats();
            this.removeOutliers = removeOutliers;
        }

        public void Clear() {
            Data.Clear();
            Stats.Reset();
            lowerBound = double.NegativeInfinity;
            upperBound = double.PositiveInfinity;
        }


        /// <summary>
        /// Add value, remove last if over size
        /// </summary>
        /// <param name="value">The value.</param>
        public void AddToFront(double value) {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            if (Count == Capacity) {
                double oldData = Data.RemoveFromBack();
            }
            Data.AddToFront(value);
            SetBounds();
            switch (removeOutliers) {
                case RemoveOutliers.Upper:
                    s = new DescriptiveStatistics(Data.Where(x => x < upperBound && !double.IsNaN(x)));
                    break;
                case RemoveOutliers.Lower:
                    s = new DescriptiveStatistics(Data.Where(x => lowerBound < x && !double.IsNaN(x)));
                    break;
                case RemoveOutliers.Both:
                    s = new DescriptiveStatistics(Data.Where(x => lowerBound < x && x < upperBound && !double.IsNaN(x)));
                    break;
                case RemoveOutliers.QPlus1:
                    s = new DescriptiveStatistics(Data.Where(x => lowerBound - 1 < x && x < upperBound + 1 && !double.IsNaN(x)));
                    break;
                default:
                    s = new DescriptiveStatistics(Data.Where(x => !double.IsNaN(x)));
                    break;
            }

            Stats.Avg = s.Mean;
            Stats.Std = s.StandardDeviation;
            Stats.Min = s.Minimum;
            Stats.Max = s.Maximum;

            //var t = sw.Elapsed;
            //if (RaceEngineerPlugin.SETTINGS.Log) {
            //    string txt = "Data = [";
            //    foreach (var a in Data) {
            //        txt += $"{a:0.000}, ";
            //    }
    //            RaceEngineerPlugin.LogInfo($@"{txt}],
    //(Min, Q1, Median, Q3, Max) = ({Min}, {Q1}, {Median}, {Q3}, {Max}),
    //(Avg, Std) = ({Avg}, {Std}),
    //(lowerBound, upperBound) = ({lowerBound}, {upperBound}),
    //Finished in {t.TotalMilliseconds}ms");
            
        }

        private void SetBounds() {
            if (Data.Count > 2) {
                var s = Statistics.FiveNumberSummary(Data);

                Stats.Q1 = s[1];
                Stats.Median = s[2];
                Stats.Q3 = s[3];

                var iqr3 = 3*(Q3 - Q1);
                lowerBound = Q1 - iqr3;
                upperBound = Q3 + iqr3;
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