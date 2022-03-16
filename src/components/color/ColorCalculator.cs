using System;
using System.Collections.Generic;

namespace RaceEngineerPlugin.Color {
    /// <summary>
    /// Linear interpolator between to points
    /// </summary>
    public class LinearInterpolator{
        private double intersection, slope, x0;

        public LinearInterpolator(double x0, double y0, double x1, double y1) {
            slope = (y1 - y0) / (x1 - x0);
            intersection = y0;
            this.x0 = x0;
        }

        public double Interpolate(double x) {
            return intersection + (x - x0) * slope;
        }

    }


    /// <summary>
    /// Linear interpolator between two colors in HSV color space.
    /// </summary>
    public class LinearColorInterpolator {
        private LinearInterpolator interH;
        private LinearInterpolator interS;
        private LinearInterpolator interV;

        public LinearColorInterpolator(double min, HSV minc, double max, HSV maxc) {
            interH = new LinearInterpolator(min, minc.H, max, maxc.H);
            interS = new LinearInterpolator(min, minc.S, max, maxc.S);
            interV = new LinearInterpolator(min, minc.V, max, maxc.V);
        }

        public HSV Interpolate(double x) {
            return new HSV(
                    (int)Math.Round(interH.Interpolate(x)),
                    interS.Interpolate(x),
                    interV.Interpolate(x)
                );
        }
    }

    /// <summary>
    /// Color interpolator between any number of colors.
    /// </summary>
    public class ColorCalculator {
        public int num_colors;
        HSV[] colors;
        double[] values;
        LinearColorInterpolator[] interpolators;


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

            num_colors = colors.Length;
            this.colors = new HSV[num_colors];
            this.values = values;

            for (int i = 0; i < num_colors; i++) {
                this.colors[i] = new HSV(colors[i]);
            }
            interpolators = new LinearColorInterpolator[num_colors];
            UpdateInterpolators();
        }

        public void UpdateInterpolation(double[] values) {
            if (num_colors != values.Length) {
                throw new Exception("There must be same number of colors and values.");
            }

            this.values = values;
            
            UpdateInterpolators();
        }

        public void UpdateInterpolation(double ideal, double delta) {
            bool even = num_colors % 2 == 0;
            int half_num = (int)num_colors / 2;

            double[] vals = new double[num_colors];
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
            for (var i = 0; i < num_colors - 1; i++) {
                interpolators[i] = new LinearColorInterpolator(values[i], colors[i], values[i + 1], colors[i + 1]);
            }
        }

        public HSV GetColor(double value) {
            if (value <= values[0]) {
                return colors[0];
            }

            if (value >= values[num_colors - 1]) {
                return colors[num_colors - 1];
            }


            for (var i = 0; i < num_colors - 1; i++) {
                if (value <= values[i+1]) {
                    return interpolators[i].Interpolate(value);
                }
            }
            return new HSV(0, 0, 0);//Cannot be actually reached
        }
    }
}