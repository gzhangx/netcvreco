using System;
using System.Text;

namespace com.veda.LinearAlg
{
    public class GMatrix
    {
        public double[][] storage { get; protected set; }
        public GMatrix(int r, int c)
        {
            rows = r;
            cols = c;
            init();
        }
        public GMatrix(double[] val, int r, int c)
        {
            rows = r;
            cols = c;
            init();
            int at = 0;
            for (int rr = 0; rr < rows; rr++)
            {
                for (int cc = 0; cc < cols; cc++)
                {
                    storage[rr][cc] = val[at++];
                }
            }
        }
        public GMatrix(double[][] val, int r, int c)
        {
            rows = r;
            cols = c;
            storage = val;
        }
        public double[] ToArray()
        {
            var r = new double[rows * cols];
            int at = 0;
            for (int rr = 0; rr < rows; rr++)
            {
                for (int cc = 0; cc < cols; cc++)
                {
                    r[at++] = storage[rr][cc];
                }
            }
            return r;
        }
        public GMatrix(double[,] v)
        {
            rows = v.GetLength(0);
            cols = v.GetLength(1);
            init();
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    storage[r][c] = v[r, c];
                }
            }
        }
        public void init()
        {
            storage = new double[rows][];
            for (var i = 0; i < rows; i++)
            {
                storage[i] = new double[cols];
            }
        }
        public GMatrix tranpose()
        {
            var r = rows;
            var c = cols;
            var newStorage = new GMatrix(c, r);
            for (var i = 0; i < r; i++)
            {
                for (var j = 0; j < c; j++)
                {
                    newStorage.storage[j][i] = storage[i][j];
                }
            }
            return newStorage;
        }
        public int cols { get; protected set; }
        public int rows { get; protected set; }

        public GMatrix dot(GMatrix m)
        {
            var r = rows;
            var mc = m.cols;
            //if (r != mc) throw new InvalidOperationException($"Cross: row {r} and col {mc} must equal");
            if (cols != m.rows) throw new InvalidOperationException($"Cross: col {cols} and row {m.rows} must equal");
            var newStorage = new double[r, mc];
            var c = cols;
            for (var i = 0; i < r; i++)
            {
                for (var j = 0; j < mc; j++)
                {
                    double total = 0;
                    for (var k = 0; k < c; k++)
                    {
                        total += storage[i][k] * m.storage[k][j];
                    }
                    newStorage[i, j] = total;
                }
            }
            return new GMatrix(newStorage);
        }

        public GMatrix div(double v)
        {
            var r = rows;
            var c = cols;
            var newStorage = new GMatrix(r,c);
            for (var i = 0; i < r; i++)
            {
                for (var j = 0; j < c; j++)
                {
                    newStorage.storage[i][j] = storage[i][j]/v;
                }
            }
            return newStorage;
        }

        public double last()
        {
            return storage[rows - 1][cols - 1];
        }

        public GMatrix noramByLast()
        {
            return div(last());
        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < cols; j++)
                {
                    if (j > 0) sb.Append(", ");
                    sb.Append(storage[i][j].ToString("0.000").PadLeft(7));
                }
                sb.Append("\n");
            }
            return sb.ToString();
        }
    }
}
