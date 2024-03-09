using System;
using System.Collections.Generic;

using KLPlugins.RaceEngineer.Car;

namespace KLPlugins.RaceEngineer.Interpolator {
    /// <summary>
    /// Linear interpolator between to points
    /// </summary>
    public class LinearInterpolator(double x0, double y0, double x1, double y1) {
        private readonly double _intersection = y0;
        private readonly double _slope = (y1 - y0) / (x1 - x0);
        private readonly double _x0 = x0;

        public double Interpolate(double x) {
            return this._intersection + (x - this._x0) * this._slope;
        }
    }

    /// <summary>
    /// Color interpolator between any number of colors.
    /// </summary>
    public class MultiPointLinearInterpolator {
        public int NumPoints { get; }

        private readonly double[] _ys;
        private double[] _xs;
        private readonly LinearInterpolator[] _interpolators;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x">Colors in HEX.</param>
        /// <param name="y">Numerical values each color corresponds to.</param>
        /// <exception cref="Exception"></exception>
        public MultiPointLinearInterpolator(double[] x, double[] y) {
            if (x.Length != y.Length) {
                throw new Exception("There must be same number of x and y points.");
            }

            this.NumPoints = x.Length;
            this._ys = y;
            this._xs = x;

            this._interpolators = new LinearInterpolator[this.NumPoints];
            this.UpdateInterpolators();
        }

        public MultiPointLinearInterpolator(Lut lut) : this([.. lut.X], [.. lut.Y]) { }

        private void UpdateInterpolators() {
            for (var i = 0; i < this.NumPoints - 1; i++) {
                this._interpolators[i] = new LinearInterpolator(this._xs[i], this._ys[i], this._xs[i + 1], this._ys[i + 1]);
            }
        }

        public double Interpolate(double value) {
            if (double.IsNaN(value)) return 0;

            if (value <= this._xs[0]) {
                return this._interpolators[0].Interpolate(value);
            }

            if (value >= this._xs[this.NumPoints - 1]) {
                return this._interpolators[this.NumPoints - 2].Interpolate(value);
            }


            for (var i = 0; i < this.NumPoints - 1; i++) {
                if (value <= this._xs[i + 1]) {
                    return this._interpolators[i].Interpolate(value);
                }
            }
            return 0;//Cannot be actually reached
        }
    }
}