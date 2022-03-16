using System;
using System.Text;

namespace RaceEngineerPlugin.Color {
    /// <summary>
    /// Stores HSV color 
    /// </summary>
    public class HSV {
        public int H { get => _h;  }
        public double S { get => _s;  }
        public double V { get => _v;  }

        private int _h;
        private double _s;
        private double _v;
        private readonly char[] digits = "0123456789ABCDEF".ToCharArray();
        private bool hexSet = false;
        private char[] hexDigits = new char[7];
        private string hex = null;

        public HSV(int h, double s, double v) {
            _h = h;
            _s = s;
            _v = v;
            hexDigits[0] = '#';
        }

        public HSV(string hex) {
            hexDigits[0] = '#';
            double r = byte.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber) / 255.0;
            double g = byte.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber) / 255.0;
            double b = byte.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber) / 255.0;

            _v = Math.Max(Math.Max(r, g), b);
            double cmin = Math.Min(Math.Min(r, g), b);
            double diff = _v - cmin;

            double hue;
            if (_v == r) {
                hue = (60 * ((g - b) / diff) + 360) % 360;
            } else if (V == g) {
                hue = (60 * ((b - r) / diff) + 120) % 360;
            } else if (V == b) {
                hue = (60 * ((r - g) / diff) + 240) % 360;
            } else {
                hue = 0.0;
            }

            if (V == 0.0) {
                _s = 0.0;
            } else {
                _s = (diff / V);
            }

            _h = (int)hue;
        }

        public void Update(int h, double s, double v) {
            _h = h;
            _s = s;
            _v = v;
            hexSet = false;
        }

        public string ToHEX() {
            if (!hexSet) {
                var c = _s * _v;
                var x = c * (1 - Math.Abs((_h / 60.0) % 2 - 1));
                var m = _v - c;

                double r, g, b;

                if (0 <= _h && _h < 60) {
                    r = c;
                    g = x;
                    b = 0.0;
                } else if (60 <= _h && _h < 120) {
                    r = x;
                    g = c;
                    b = 0.0;
                } else if (120 <= _h && _h < 180) {
                    r = 0;
                    g = c;
                    b = x;
                } else if (180 <= _h && _h < 240) {
                    r = 0;
                    g = x;
                    b = c;
                } else if (240 <= _h && _h < 300) {
                    r = x;
                    g = 0;
                    b = c;
                } else if (300 <= _h && _h < 360) {
                    r = c;
                    g = 0;
                    b = x;
                } else {
                    r = 0.0;
                    g = 0.0;
                    b = 0.0;
                }

                var rb = (byte)Math.Floor((r + m) * 255);
                var gb = (byte)Math.Floor((g + m) * 255);
                var bb = (byte)Math.Floor((b + m) * 255);

                // Algorith from here https://www.baeldung.com/java-byte-arrays-hex-strings
                hexDigits[1] = digits[(rb >> 4) & 0xF];
                hexDigits[2] = digits[rb & 0xF];
                hexDigits[3] = digits[(gb >> 4) & 0xF];
                hexDigits[4] = digits[gb & 0xF];
                hexDigits[5] = digits[(bb >> 4) & 0xF];
                hexDigits[6] = digits[bb & 0xF];
                hex = new string(hexDigits);
            }
            return hex;
        }
    }

}
