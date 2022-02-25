using System;

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
}
