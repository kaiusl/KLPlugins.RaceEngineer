using System;
using System.Collections.Generic;
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
        public Stats.Stats Stats { get; }
        public double Min { get => Stats.Min; }
        public double Max { get => Stats.Max; }
        public double Avg { get => Stats.Avg; }
        public double Std { get => Stats.Std; }

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
            sum += value;
            sumOfSquares += value * value;

            Stats.Avg = sum / Count;
            SetMin(value);
            SetMax(value);
            Stats.Std = Math.Sqrt(sumOfSquares / Count - Avg*Avg);
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
            } else if (minId == -1 || value < Min) { // Otherwise we can simply compare with current minimum value
                Stats.Min = value;
                minId = 0;
            } else { // If new value was larger, index of minimum value is shifted by one
                minId++;
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
            } else if (maxId == -1 || value > Max) { // Otherwise we can simply compare with current minimum value
                Stats.Max = value;
                maxId = 0;
            } else { // If new value was smaller, index of max value is shifted by one
                maxId++;
            }
        }

    }
}