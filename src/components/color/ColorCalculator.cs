using System;
using System.Collections.Generic;

namespace RaceEngineerPlugin.Color {
    public class Point { 
        public double x, y;

        public Point(double x, double y) { this.x = x; this.y = y;  }
    }

    /// <summary>
    /// Linear interpolator between to points
    /// </summary>
    public class LinearInterpolator{
        private double intersection, slope, x0;

        public LinearInterpolator(Point p0, Point p1) {
            slope = (p1.y - p0.y) / (p1.x - p0.x);
            intersection = p0.y;// - p0.x * slope;
            x0 = p0.x;
        }

        public double Interpolate(double x) {
            return intersection + (x - x0) * slope;
        }

    }

    /// <summary>
    /// Store color and value it corresponds to
    /// </summary>
    public class ColorPoint {
        public Color Color { get; set; }
        public double Value { get; set; }

        public ColorPoint(Color c, double v) {
            Color = c;
            Value = v;
        }
    }


    /// <summary>
    /// Linear interpolator between two colors in HSV color space.
    /// </summary>
    public class LinearColorInterpolator {
        private LinearInterpolator[] interpolators = new LinearInterpolator[3];

        public LinearColorInterpolator(ColorPoint min, ColorPoint max) {
            for (int i = 0; i < 3; i++) {
                interpolators[i] = new LinearInterpolator(
                    new Point(min.Value, min.Color.HSV[i]),
                    new Point(max.Value, max.Color.HSV[i])
                    );
            }
        }

        public Color Interpolate(double x) {
            return new Color(
                new HSV(
                    (int)Math.Round(interpolators[0].Interpolate(x)),
                    interpolators[1].Interpolate(x),
                    interpolators[2].Interpolate(x)
                )
            );
        }
    }

    /// <summary>
    /// Color interpolator between any number of colors.
    /// </summary>
    public class ColorCalculator {
        public int num_colors;
        ColorPoint[] cp;
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
            cp = new ColorPoint[num_colors];

            for (int i = 0; i < num_colors; i++) {
                cp[i] = new ColorPoint(new Color(colors[i]), values[i]);
            }
            interpolators = new LinearColorInterpolator[num_colors];
            UpdateInterpolators();
        }

      
        public void UpdateInterpolation(double[] values) {
            if (num_colors != values.Length) {
                throw new Exception("There must be same number of colors and values.");
            }

            for (var i = 0; i < num_colors; i++) {
                cp[i].Value = values[i];
            }
            
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
                interpolators[i] = new LinearColorInterpolator(cp[i], cp[i + 1]);
            }
        }

        public string GetHexColor(double value) {
            if (value <= cp[0].Value) {
                return cp[0].Color.Hex;
            }

            if (value >= cp[num_colors - 1].Value) {
                return cp[num_colors - 1].Color.Hex;
            }


            for (var i = 0; i < num_colors - 1; i++) {
                if (value <= cp[i+1].Value) {
                    return interpolators[i].Interpolate(value).Hex;
                }
            }
            return null; // Cannot actually be reached
        }
    }
}