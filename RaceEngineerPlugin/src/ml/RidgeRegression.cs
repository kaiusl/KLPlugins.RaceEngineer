using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using MathNet.Numerics.LinearAlgebra;

namespace KLPlugins.RaceEngineer.ML {

    /// <summary>
    /// Ridge regression algorithm
    /// </summary>
    internal class RidgeRegression {
        Vector<double> y;
        Matrix<double> x;
        Vector<double> w;

        internal RidgeRegression(double[][] x, double[] y)
            : this(Matrix<double>.Build.DenseOfRows(x), Vector<double>.Build.Dense(y)) { }

        internal RidgeRegression(List<double[]> x, List<double> y)
            : this(Matrix<double>.Build.DenseOfRows(x), Vector<double>.Build.DenseOfEnumerable(y)) { }

        internal RidgeRegression(Matrix<double> x, Vector<double> y) {
            this.x = x;
            this.y = y;

            Debug.Assert(this.x.RowCount == this.y.Count, $"'y' and 'X' have different number of samples. Respectively {this.y.Count} and {this.x.RowCount}.");

            for (int i = 0; i < this.x.RowCount; i++) {
                Debug.Assert(this.x[i, 0] == 1, $"Features must be in homogeneous coordinates. First column must be 1. Found {this.x[i, 0]} in sample {i}.");
            }

            this.w = this.GetWeights(); // compiler cannot see that we assign in this.Fit(), so inline it
        }

        internal void Fit() {
            this.w = this.GetWeights();
        }

        internal Vector<double> GetWeights() {
            return (this.x.Transpose().Multiply(this.x) + Matrix<double>.Build.DenseIdentity(this.x.ColumnCount)).Inverse().Multiply(this.x.Transpose().Multiply(this.y));
        }

        internal double Predict(double[] v) {
            Debug.Assert(v.Length == this.w.Count - 1, $"Model has {this.w.Count - 1} features. Provided input has {v.Length} features.");

            var res = this.w[0];
            for (int i = 0; i < v.Length; i++) {
                res += v[i] * this.w[i + 1];
            }
            return res;
        }

        internal void AddNewData(double[] x, double y) {
            Trace.Assert(x[0] == 1, $"Features must be in homogeneous coordinates. First column must be 1.");
            this.y = Vector<double>.Build.DenseOfEnumerable(this.y.Append(y));
            var newRow = Vector<double>.Build.Dense(x);
            this.x = this.x.InsertRow(this.x.RowCount, newRow);
        }
    }
}