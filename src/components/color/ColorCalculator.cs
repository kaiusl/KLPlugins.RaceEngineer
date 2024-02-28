using System;
using System.Collections.Generic;

namespace KLPlugins.RaceEngineer.Color {
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
        public int NumPoints;

        private readonly double[] _xs;
        private double[] _ys;
        private readonly LinearInterpolator[] _interpolators;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x">Colors in HEX.</param>
        /// <param name="y">Numerical values each color corresponds to.</param>
        /// <exception cref="Exception"></exception>
        public MultiPointLinearInterpolator(double[] x, double[] y) {
            if (x.Length != y.Length) {
                throw new Exception("There must be same number of colors and values.");
            }

            this.NumPoints = x.Length;
            this._xs = x;
            this._ys = y;

            this._interpolators = new LinearInterpolator[this.NumPoints];
            this.UpdateInterpolators();
        }

        // public void UpdateInterpolation(double[] values) {
        //     if (this.NumPoints != values.Length) {
        //         throw new Exception("There must be same number of colors and values.");
        //     }

        //     this._ys = values;

        //     this.UpdateInterpolators();
        // }

        // public void UpdateInterpolation(double ideal, double delta) {
        //     bool even = this.NumPoints % 2 == 0;
        //     int half_num = this.NumPoints / 2;

        //     double[] vals = new double[this.NumPoints];
        //     if (!even) {
        //         for (int i = -half_num; i <= half_num; i++) {
        //             vals[i + half_num] = ideal + i * delta;
        //         }
        //     } else {
        //         for (int i = -half_num; i < half_num; i++) {
        //             if (i < 0) {
        //                 vals[i + half_num] = ideal + i * delta;
        //             } else {
        //                 vals[i + half_num] = ideal + (i + 1) * delta;
        //             }
        //         }
        //     }

        //     this.UpdateInterpolation(vals);
        // }

        private void UpdateInterpolators() {
            for (var i = 0; i < this.NumPoints - 1; i++) {
                this._interpolators[i] = new LinearInterpolator(this._ys[i], this._xs[i], this._ys[i + 1], this._xs[i + 1]);
            }
        }

        public double Interpolate(double value) {
            if (double.IsNaN(value)) return 0;

            if (value <= this._ys[0]) {
                return this._xs[0];
            }

            if (value >= this._ys[this.NumPoints - 1]) {
                return this._xs[this.NumPoints - 1];
            }


            for (var i = 0; i < this.NumPoints - 1; i++) {
                if (value <= this._ys[i + 1]) {
                    return this._interpolators[i].Interpolate(value);
                }
            }
            return 0;//Cannot be actually reached
        }
    }
}