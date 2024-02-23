﻿using System;
using System.Text;

namespace KLPlugins.RaceEngineer.Color {
    /// <summary>
    /// Stores HSV color 
    /// </summary>
    public class HSV {
        public int H { get => this._h; }
        public double S { get => this._s; }
        public double V { get => this._v; }

        private int _h;
        private double _s;
        private double _v;
        private readonly char[] _digits = "0123456789ABCDEF".ToCharArray();
        private bool _hexSet = false;
        private char[] _hexDigits = new char[7];
        private string _hex = null;

        public HSV(int h, double s, double v) {
            this._h = h;
            this._s = s;
            this._v = v;
            this._hexDigits[0] = '#';
        }

        public HSV(string hex) {
            this._hexDigits[0] = '#';
            double r = byte.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber) / 255.0;
            double g = byte.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber) / 255.0;
            double b = byte.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber) / 255.0;

            this._v = Math.Max(Math.Max(r, g), b);
            double cmin = Math.Min(Math.Min(r, g), b);
            double diff = this._v - cmin;

            double hue;
            if (this._v == r) {
                hue = (60 * ((g - b) / diff) + 360) % 360;
            } else if (this.V == g) {
                hue = (60 * ((b - r) / diff) + 120) % 360;
            } else if (this.V == b) {
                hue = (60 * ((r - g) / diff) + 240) % 360;
            } else {
                hue = 0.0;
            }

            if (this.V == 0.0) {
                this._s = 0.0;
            } else {
                this._s = (diff / this.V);
            }

            this._h = (int)hue;
        }

        public void Update(int h, double s, double v) {
            this._h = h;
            this._s = s;
            this._v = v;
            this._hexSet = false;
        }

        public string ToHEX() {
            if (!this._hexSet) {
                var c = this._s * this._v;
                var x = c * (1 - Math.Abs((this._h / 60.0) % 2 - 1));
                var m = this._v - c;

                double r, g, b;

                if (0 <= this._h && this._h < 60) {
                    r = c;
                    g = x;
                    b = 0.0;
                } else if (60 <= this._h && this._h < 120) {
                    r = x;
                    g = c;
                    b = 0.0;
                } else if (120 <= this._h && this._h < 180) {
                    r = 0;
                    g = c;
                    b = x;
                } else if (180 <= this._h && this._h < 240) {
                    r = 0;
                    g = x;
                    b = c;
                } else if (240 <= this._h && this._h < 300) {
                    r = x;
                    g = 0;
                    b = c;
                } else if (300 <= this._h && this._h < 360) {
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
            return this._hex;
        }
    }

}