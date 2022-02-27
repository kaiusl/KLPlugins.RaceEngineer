using System;
using System.Collections.Generic;
using System.Linq;
using Nito.Collections;

namespace RaceEngineerPlugin.Deque {

    /// <summary>
    /// Deque that never grows larger than given size.
    /// 
    /// In our case we need some data structure to store previous lap values (lap times, fuel) 
    /// in which we need to push to the front (so that newest values are first) and remove from the back (eg remove oldest values).
    /// We also only want to store certain number of values, hence the fixed size bit.
    /// </summary>
    public class FixedSizeDeque {
        public Deque<double> Data { get; }
        public int Capacity { get => Data.Capacity; }
        public int Count {  get => Data.Count; }

        public FixedSizeDeque(int size) {
            Data = new Deque<double>(size);
        }

        public double? AddToFront(double value) {
            double? lastVal = null;
            if (Data.Count == Data.Capacity) {
                lastVal = Data.RemoveFromBack();
            }

            Data.AddToFront(value);

            return lastVal;
        }

        public void Clear() { 
            Data.Clear();
        }

        public double this[int key] {
            get => Data[key];
        }

    }

    /// <summary>
    /// We also want to have some statistics over the previous values. 
    /// </summary>
    public class FixedSizeDequeStats : FixedSizeDeque {
        private const string TAG = RaceEngineerPlugin.PLUGIN_NAME + " (FixedSizeDequeStats): ";
        public Stats.Stats Stats { get; }
        public double Min { get => Stats.Min; }
        public double Max { get => Stats.Max; }
        public double Avg { get => Stats.Avg; }
        public double Std { get => Stats.Std; }

        private double q1 = -1.0;
        private double q3 = -1.0;

        private double lowerBound = -1.0;
        private double upperBound = -1.0;

        private double sum = 0.0;
        private double sumOfSquares = 0.0;
        private int minId = -1;
        private int maxId = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedSizeDequeStats"/> class.
        /// </summary>
        /// <param name="size">The size.</param>
        public FixedSizeDequeStats(int size) : base(size) {
            Stats = new Stats.Stats();
        }

        new public void Clear() {
            base.Clear();
            Stats.Reset();
            sum = 0.0;
            sumOfSquares = 0.0;
            minId = -1;
            maxId = -1;
            q1 = -1;
            q3 = -1;
            lowerBound = -1;
            upperBound = -1;
        }

        // Add value, remove last if over size
        /// <summary>
        /// Enqueues the.
        /// </summary>
        /// <param name="value">The value.</param>
        new public void AddToFront(double value) {
            if (Count == Capacity) {
                double oldData = Data.RemoveFromBack();
                sum -= oldData;
                sumOfSquares -= oldData * oldData;
            }
            Data.AddToFront(value);
            string txt = "Data = [";
            foreach (var a in Data) {
                txt += $"{a:0.000}, ";
            }
            LogInfo(txt + "]");
            sum += value;
            sumOfSquares += value * value;

            Stats.Avg = sum / Count;
            SetMin(value);
            SetMax(value);
            Stats.Std = Math.Sqrt(sumOfSquares / Count - Avg*Avg);
            SetBounds(value);
            LogInfo($@"Avg set to '{Stats.Avg}', 
    std set to '{Stats.Std}', 
    (q1, q3) set to ('{q1}', '{q3}'),
    (lowerBound, upperBound) set to ('{lowerBound}', '{upperBound}')");
        }

        private void SetBounds(double value) {
            var b = Data.ToArray();
            Array.Sort(b);

            if (b.Length > 2) {
                var i = b.Length / 2;
                q1 = GetMedian(b.Take(i).ToArray());
                q3 = GetMedian(b.Reverse().Take(i).ToArray());

                var iqr = q3 - q1;

                lowerBound = q1 - 3 * iqr;
                upperBound = q3 + 3 * iqr;
            }  
        }

        private double GetMedian(double[] v) {
            if (v.Length % 2 == 0) {
                var i = v.Length / 2;
                return (v[i - 1] + v[i]) / 2.0;
            } else {
                return v[v.Length / 2];
            }

        }

        private void SetMin(double value) {
            // If previous minimum value was last in queue, we remove that value. Now we don't know the smallest/largest from rest. Need to traverse whole array.
            if (minId == Capacity - 1) {
                Stats.Min = Data[0];
                minId = 0;
                for (int i = 1; i < Capacity; i++) {
                    double d = Data[i];
                    if (d <= Min) {
                        Stats.Min = d;
                        minId = i;
                    }
                }
                LogInfo($"Min set to '{Stats.Min}', minId = '{minId}'");
            } else if (minId == -1 || value < Min) { // Otherwise we can simply compare with current minimum value
                Stats.Min = value;
                minId = 0;
                LogInfo($"Min set to '{Stats.Min}'.");
            } else { // If new value was larger, index of minimum value is shifted by one
                minId++;
                LogInfo($"New value not min, minId = '{minId}'");
            }
        }

        private void SetMax(double value) {
            // If previous minimum value was last in queue, we remove that value. Now we don't know the smallest/largest from rest. Need to traverse whole array.
            if (maxId == Capacity - 1) {
                Stats.Max = Data[0];
                maxId = 0;
                for (int i = 1; i < Count; i++) {
                    double d = Data[i];
                    if (d >= Max) {// At this point whole array must be filled, so we don't need to check for NaNs.
                        Stats.Max = d;
                        maxId = i;
                    }
                }
                LogInfo($"Max set to '{Stats.Max}', maxId = '{maxId}'");
            } else if (maxId == -1 || value > Max) { // Otherwise we can simply compare with current minimum value
                Stats.Max = value;
                maxId = 0;
                LogInfo($"Max set to '{Stats.Max}'");
            } else { // If new value was smaller, index of max value is shifted by one
                maxId++;
                LogInfo($"New value not max, maxId = '{maxId}'");
            }
        }

        private void LogInfo(string msq) {
            if (RaceEngineerPlugin.SETTINGS.Log) {
                SimHub.Logging.Current.Info(TAG + msq);
            }
        }

    }
}