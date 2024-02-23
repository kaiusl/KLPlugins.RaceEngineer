using System;
using System.Text;

namespace KLPlugins.RaceEngineer.Color {
    /// <summary>
    /// Stores HSV color 
    /// </summary>
    public class HSV {
        public int H { get; private set; }
        public double S { get; private set; }
        public double V { get; private set; }

        private readonly char[] _digits = "0123456789ABCDEF".ToCharArray();
        private bool _hexSet = false;
        private readonly char[] _hexDigits = new char[7];
        private string? _hex = null;

        public HSV(int h, double s, double v) {
            this.H = h;
            this.S = s;
            this.V = v;
            this._hexDigits[0] = '#';
        }

        public HSV(string hex) {
            this._hexDigits[0] = '#';
            double r = byte.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber) / 255.0;
            double g = byte.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber) / 255.0;
            double b = byte.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber) / 255.0;

            this.V = Math.Max(Math.Max(r, g), b);
            double cmin = Math.Min(Math.Min(r, g), b);
            double diff = this.V - cmin;

            double hue;
            if (this.V == r) {
                hue = (60 * ((g - b) / diff) + 360) % 360;
            } else if (this.V == g) {
                hue = (60 * ((b - r) / diff) + 120) % 360;
            } else if (this.V == b) {
                hue = (60 * ((r - g) / diff) + 240) % 360;
            } else {
                hue = 0.0;
            }

            if (this.V == 0.0) {
                this.S = 0.0;
            } else {
                this.S = (diff / this.V);
            }

            this.H = (int)hue;
        }

        public void Update(int h, double s, double v) {
            this.H = h;
            this.S = s;
            this.V = v;
            this._hexSet = false;
        }

        public string ToHEX() {
            if (!this._hexSet) {
                var c = this.S * this.V;
                var x = c * (1 - Math.Abs((this.H / 60.0) % 2 - 1));
                var m = this.V - c;

                double r, g, b;

                if (0 <= this.H && this.H < 60) {
                    r = c;
                    g = x;
                    b = 0.0;
                } else if (60 <= this.H && this.H < 120) {
                    r = x;
                    g = c;
                    b = 0.0;
                } else if (120 <= this.H && this.H < 180) {
                    r = 0;
                    g = c;
                    b = x;
                } else if (180 <= this.H && this.H < 240) {
                    r = 0;
                    g = x;
                    b = c;
                } else if (240 <= this.H && this.H < 300) {
                    r = x;
                    g = 0;
                    b = c;
                } else if (300 <= this.H && this.H < 360) {
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
                this._hexDigits[1] = this._digits[(rb >> 4) & 0xF];
                this._hexDigits[2] = this._digits[rb & 0xF];
                this._hexDigits[3] = this._digits[(gb >> 4) & 0xF];
                this._hexDigits[4] = this._digits[gb & 0xF];
                this._hexDigits[5] = this._digits[(bb >> 4) & 0xF];
                this._hexDigits[6] = this._digits[bb & 0xF];
                this._hex = new string(this._hexDigits);
            }

            // above if will set this._hex if it was null
            return this._hex!;
        }
    }

}