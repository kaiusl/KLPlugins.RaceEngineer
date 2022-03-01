using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RaceEngineerPlugin.ML {

    public class RidgeRegression {
        Vector<double> y;
        Matrix<double> X;
        Vector<double> w;

        public RidgeRegression(double[][] X, double[] y)
            : this(Matrix<double>.Build.DenseOfRows(X), Vector<double>.Build.Dense(y)) { }

        public RidgeRegression(List<double[]> X, List<double> y)
            : this(Matrix<double>.Build.DenseOfRows(X), Vector<double>.Build.DenseOfEnumerable(y)) { }

        public RidgeRegression(Matrix<double> X, Vector<double> y) {
            this.X = X;
            this.y = y;

            if (this.X.RowCount != this.y.Count) {
                throw new Exception($"'y' and 'X' have different number of samples. Respectively {this.y.Count} and {this.X.RowCount}.");
            }

            for (int i = 0; i < X.RowCount; i++) {
                if (this.X[i, 0] != 1) {
                    throw new Exception($"Features must be in homogeneous coordinates. First column must be 1.");
                }
            }

            this.w = Fit();
        }

        public Vector<double> Fit() {
            var v = ((X.Transpose().Multiply(X) + Matrix<double>.Build.DenseIdentity(X.ColumnCount)).Inverse()).Multiply(X.Transpose().Multiply(y));
            //SimHub.Logging.Current.Info($"Fit y = {y}");
            //SimHub.Logging.Current.Info($"Fit x = {X}");
            //SimHub.Logging.Current.Info($"Fit coeffs = {v}");
            return v;
        }

        public double Predict(double[] v) {
            var res = w[0];
            if (v.Length == w.Count - 1) {
                for (int i = 0; i < v.Length; i++) {
                    res += v[i] * w[i + 1];
                }
                return res;
            } else {
                throw new Exception($"Model has {w.Count - 1} features. Provided input has {v.Length} features.");
            }
        }

        public void AddNewData(double y, double[] x) {
            this.y = Vector<double>.Build.DenseOfEnumerable(this.y.Append(y));

            if (x[0] != 1) {
                throw new Exception($"Features must be in homogeneous coordinates. First column must be 1.");
            }
            var newRow = Vector<double>.Build.Dense(x);
            X = X.InsertRow(X.RowCount, newRow);
        }
    }
}