using System;
using System.Collections.Generic;

namespace RaceEngineerPlugin.Color {

    /// <summary>
    /// Stores RGB color 
    /// </summary>
    public struct RGB {
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }

        public RGB(int r, int g, int b) {
            R = r;
            G = g;
            B = b;
        }

        public double this[int key] {
            get {
                switch (key) {
                    case 0:
                        return R;
                    case 1:
                        return G;
                    case 2:
                        return B;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// Stores HSV color 
    /// </summary>
    public struct HSV {
        public int H { get; set; }
        public double S { get; set; }
        public double V { get; set; }

        public HSV(int h, double s, double v) {
            H = h;
            S = s;
            V = v;
        }

        public double this[int key] {
            get {
                switch (key) {
                    case 0:
                        return H;
                    case 1:
                        return S;
                    case 2:
                        return V;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// Stores both RGB and Hex color values.
    /// </summary>
    public class Color {
        public string Hex { get; set; }
        public RGB RGB { get; set; }
        public HSV HSV { get; set; }

        public Color(string hex) {
            Hex = hex;
            RGB = HexToRGB(hex);
            HSV = RGBToHSV(RGB);
        }

        public Color(RGB rgb) {
            RGB = rgb;
            HSV = RGBToHSV(rgb);
            Hex = RGBToHex(rgb);
        }

        public Color(HSV hsv) {
            HSV = hsv;
            RGB = HSVToRGB(hsv);
            Hex = RGBToHex(RGB);
        }

        public static RGB HexToRGB(string hex) {
            return new RGB(
                int.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                int.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                int.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber)
                );
        }

        public static string RGBToHex(RGB rgb) {
            return String.Format("#{0}{1}{2}", rgb.R.ToString("X2"), rgb.G.ToString("X2"), rgb.B.ToString("X2"));
        }

        public static HSV RGBToHSV(RGB rgb) {
            double r = rgb.R / 255.0;
            double g = rgb.G / 255.0;
            double b = rgb.B / 255.0;

            double cmax = Math.Max(Math.Max(r, g), b);
            double cmin = Math.Min(Math.Min(r, g), b);
            double diff = cmax - cmin;

            double hue;
            if (cmax == r) {
                hue = (60 * ((g - b) / diff) + 360) % 360;
            } else if (cmax == g) {
                hue = (60 * ((b - r) / diff) + 120) % 360;
            } else if (cmax == b) {
                hue = (60 * ((r - g) / diff) + 240) % 360;
            } else {
                hue = 0.0;
            }

            double sat;
            if (cmax == 0.0) {
                sat = 0.0;
            } else {
                sat = (diff / cmax);
            }

            return new HSV((int)hue, sat, cmax);
        }

        public static RGB HSVToRGB(HSV hsv) {
            var c = hsv.S * hsv.V;
            var x = c * (1 - Math.Abs((hsv.H / 60.0) % 2 - 1));
            var m = hsv.V - c;

            double r, g, b;

            if (0 <= hsv.H && hsv.H < 60) {
                r = c;
                g = x;
                b = 0.0;
            } else if (60 <= hsv.H && hsv.H < 120) {
                r = x;
                g = c;
                b = 0.0;
            } else if (120 <= hsv.H && hsv.H < 180) {
                r = 0;
                g = c;
                b = x;
            } else if (180 <= hsv.H && hsv.H < 240) {
                r = 0;
                g = x;
                b = c;
            } else if (240 <= hsv.H && hsv.H < 300) {
                r = x;
                g = 0;
                b = c;
            } else if (300 <= hsv.H && hsv.H < 360) {
                r = c;
                g = 0;
                b = x;
            } else {
                r = 0.0;
                g = 0.0;
                b = 0.0;
            }

            r = Math.Floor((r + m) * 255);
            g = Math.Floor((g + m) * 255);
            b = Math.Floor((b + m) * 255);

            return new RGB((int)r, (int)g, (int)b);
        }

        public static HSV HexToHSV(string hex) { 
            return RGBToHSV(HexToRGB(hex));
        }

        public static string HSVToHex(HSV hsv) {
            return RGBToHex(HSVToRGB(hsv));
        }
    }



    /// <summary>
    /// 
    /// </summary>
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