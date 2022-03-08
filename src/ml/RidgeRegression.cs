using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RaceEngineerPlugin.ML {

    /// <summary>
    /// Ridge regression algorithm
    /// </summary>
    public class RidgeRegression {
        Vector<double> y;
        Matrix<double> x;
        Vector<double> w;

        public RidgeRegression(double[][] x, double[] y)
            : this(Matrix<double>.Build.DenseOfRows(x), Vector<double>.Build.Dense(y)) { }

        public RidgeRegression(List<double[]> x, List<double> y)
            : this(Matrix<double>.Build.DenseOfRows(x), Vector<double>.Build.DenseOfEnumerable(y)) { }

        public RidgeRegression(Matrix<double> x, Vector<double> y) {
            this.x = x;
            this.y = y;

            Trace.Assert(this.x.RowCount == this.y.Count, $"'y' and 'X' have different number of samples. Respectively {this.y.Count} and {this.x.RowCount}.");

            for (int i = 0; i < this.x.RowCount; i++) {
                Trace.Assert(this.x[i, 0] == 1, $"Features must be in homogeneous coordinates. First column must be 1. Found {this.x[i, 0]} in sample {i}.");
            }

            Fit();
        }

        public void Fit() {
            this.w = ((x.Transpose().Multiply(x) + Matrix<double>.Build.DenseIdentity(x.ColumnCount)).Inverse()).Multiply(x.Transpose().Multiply(y));
        }

        public double Predict(double[] v) {
            Trace.Assert(v.Length == w.Count - 1, $"Model has {w.Count - 1} features. Provided input has {v.Length} features.");

            var res = w[0];
            for (int i = 0; i < v.Length; i++) {
                res += v[i] * w[i + 1];
            }
            return res;
        }

        public void AddNewData(double[] x, double y) {
            Trace.Assert(x[0] == 1, $"Features must be in homogeneous coordinates. First column must be 1.");
            this.y = Vector<double>.Build.DenseOfEnumerable(this.y.Append(y));
            var newRow = Vector<double>.Build.Dense(x);
            this.x = this.x.InsertRow(this.x.RowCount, newRow);
        }
    }
}