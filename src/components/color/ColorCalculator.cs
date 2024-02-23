using System;
using System.Collections.Generic;

namespace KLPlugins.RaceEngineer.Color {
    /// <summary>
    /// Linear interpolator between to points
    /// </summary>
    public class LinearInterpolator {
        private double _intersection, _slope, _x0;

        public LinearInterpolator(double x0, double y0, double x1, double y1) {
            this._slope = (y1 - y0) / (x1 - x0);
            this._intersection = y0;
            this._x0 = x0;
        }

        public double Interpolate(double x) {
            return this._intersection + (x - this._x0) * this._slope;
        }

    }


    /// <summary>
    /// Linear interpolator between two colors in HSV color space.
    /// </summary>
    public class LinearColorInterpolator {
        private LinearInterpolator _interH;
        private LinearInterpolator _interS;
        private LinearInterpolator _interV;

        public LinearColorInterpolator(double min, HSV minc, double max, HSV maxc) {
            this._interH = new LinearInterpolator(min, minc.H, max, maxc.H);
            this._interS = new LinearInterpolator(min, minc.S, max, maxc.S);
            this._interV = new LinearInterpolator(min, minc.V, max, maxc.V);
        }

        public HSV Interpolate(double x) {
            return new HSV(
                    (int)Math.Round(this._interH.Interpolate(x)),
                    this._interS.Interpolate(x),
                    this._interV.Interpolate(x)
                );
        }
    }

    /// <summary>
    /// Color interpolator between any number of colors.
    /// </summary>
    public class ColorCalculator {
        public int NumColor;

        private HSV[] _colors;
        private double[] _values;
        private LinearColorInterpolator[] _interpolators;

        private static HSV DefColor;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="colors">Colors in HEX.</param>
        /// <param name="values">Numerical values each color corresponds to.</param>
        /// <exception cref="Exception"></exception>
        public ColorCalculator(string[] colors, double[] values) {
            if (colors.Length != values.Length) {
                throw new Exception("There must be same number of colors and values.");
            }

            this.NumColor = colors.Length;
            this._colors = new HSV[this.NumColor];
            this._values = values;

            for (int i = 0; i < this.NumColor; i++) {
                this._colors[i] = new HSV(colors[i]);
            }
            this._interpolators = new LinearColorInterpolator[this.NumColor];
            this.UpdateInterpolators();

            DefColor = new HSV(RaceEngineerPlugin.DefColor);
        }

        public void UpdateInterpolation(double[] values) {
            if (this.NumColor != values.Length) {
                throw new Exception("There must be same number of colors and values.");
            }

            this._values = values;

            this.UpdateInterpolators();
        }

        public void UpdateInterpolation(double ideal, double delta) {
            bool even = this.NumColor % 2 == 0;
            int half_num = (int)this.NumColor / 2;

            double[] vals = new double[this.NumColor];
            if (!even) {
                for (int i = -half_num; i <= half_num; i++) {
                    vals[i + half_num] = ideal + i * delta;
                }
            } else {
                for (int i = -half_num; i < half_num; i++) {
                    if (i < 0) {
                        vals[i + half_num] = ideal + i * delta;
                    } else {
                        vals[i + half_num] = ideal + (i + 1) * delta;
                    }
                }
            }

            this.UpdateInterpolation(vals);
        }

        private void UpdateInterpolators() {
            for (var i = 0; i < this.NumColor - 1; i++) {
                this._interpolators[i] = new LinearColorInterpolator(this._values[i], this._colors[i], this._values[i + 1], this._colors[i + 1]);
            }
        }

        public HSV GetColor(double value) {
            if (double.IsNaN(value)) return DefColor;

            if (value <= this._values[0]) {
                return this._colors[0];
            }

            if (value >= this._values[this.NumColor - 1]) {
                return this._colors[this.NumColor - 1];
            }


            for (var i = 0; i < this.NumColor - 1; i++) {
                if (value <= this._values[i + 1]) {
                    return this._interpolators[i].Interpolate(value);
                }
            }
            return new HSV(0, 0, 0);//Cannot be actually reached
        }
    }
}