using System;
using System.Collections.Generic;

namespace RaceEngineerPlugin.Color {
    /// <summary>
    /// Linear interpolator between to points
    /// </summary>
    public class LinearInterpolator{
        private double _intersection, _slope, _x0;

        public LinearInterpolator(double x0, double y0, double x1, double y1) {
            _slope = (y1 - y0) / (x1 - x0);
            _intersection = y0;
            this._x0 = x0;
        }

        public double Interpolate(double x) {
            return _intersection + (x - _x0) * _slope;
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
            _interH = new LinearInterpolator(min, minc.H, max, maxc.H);
            _interS = new LinearInterpolator(min, minc.S, max, maxc.S);
            _interV = new LinearInterpolator(min, minc.V, max, maxc.V);
        }

        public HSV Interpolate(double x) {
            return new HSV(
                    (int)Math.Round(_interH.Interpolate(x)),
                    _interS.Interpolate(x),
                    _interV.Interpolate(x)
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

            NumColor = colors.Length;
            this._colors = new HSV[NumColor];
            this._values = values;

            for (int i = 0; i < NumColor; i++) {
                this._colors[i] = new HSV(colors[i]);
            }
            _interpolators = new LinearColorInterpolator[NumColor];
            UpdateInterpolators();

            DefColor = new HSV(RaceEngineerPlugin.DefColor);
        }

        public void UpdateInterpolation(double[] values) {
            if (NumColor != values.Length) {
                throw new Exception("There must be same number of colors and values.");
            }

            this._values = values;
            
            UpdateInterpolators();
        }

        public void UpdateInterpolation(double ideal, double delta) {
            bool even = NumColor % 2 == 0;
            int half_num = (int)NumColor / 2;

            double[] vals = new double[NumColor];
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

            UpdateInterpolation(vals);
        }

        private void UpdateInterpolators() {
            for (var i = 0; i < NumColor - 1; i++) {
                _interpolators[i] = new LinearColorInterpolator(_values[i], _colors[i], _values[i + 1], _colors[i + 1]);
            }
        }

        public HSV GetColor(double value) {
            if (double.IsNaN(value)) return DefColor;

            if (value <= _values[0]) {
                return _colors[0];
            }

            if (value >= _values[NumColor - 1]) {
                return _colors[NumColor - 1];
            }


            for (var i = 0; i < NumColor - 1; i++) {
                if (value <= _values[i+1]) {
                    return _interpolators[i].Interpolate(value);
                }
            }
            return new HSV(0, 0, 0);//Cannot be actually reached
        }
    }
}