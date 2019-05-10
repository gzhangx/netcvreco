using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;


namespace MatrixTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var r = new GMatrix(new double[,] { { 1, 2 }, { 3, 4 } , { 1, 1 } }).cross(new GMatrix(new double[,] { { 1, 1 ,1}, { 3, 4 ,1} }));
            Console.WriteLine(r);
            new Calib().Calc(new PointF[] {
                new Point(1,2),
                new Point(3,4),
                new Point(5,4),
                new Point(6,4),
                new Point(7,4),
                new Point(8,4),
                new Point(9,4),
                new Point(10,4),
                new Point(11,4),
                new Point(12,4),
                new Point(13,4),
                new Point(14,4),
            },
            new PointF[] {
                new Point(1,2),
                new Point(3,4),
                new Point(3,4),
                new Point(3,4),
                new Point(3,4),
                new Point(3,4),
                new Point(3,4),
                new Point(3,4),
                new Point(3,4),
                new Point(3,4),
                new Point(3,4),
                new Point(3,4),
            }
            );

            var res = svd.SVD(new double[,] {
                {1.0,2, 3 },
                {2.0,1,1 },
                {2.0,2,2 },
            });

            foreach (var q in res.q)
            {
                Console.WriteLine(q);
            }
            Console.WriteLine(new GMatrix(res.u));
            Console.WriteLine(new GMatrix(res.v));
        }

        

        
    }


    public class Calib
    {
        public void Calc(PointF[] points1, PointF[] points2)
        {
            double[,] storage = new double[points1.Length, 9];
            for (var i = 0; i < points1.Length; i++)
            {
                var pp1 = points1[i];
                var pp2 = points2[i];
                var y1 = pp1.X;
                var y2 = pp1.Y;
                var p1 = pp2.X;
                var p2 = pp2.Y;
                storage[i, 0] = y1 * p1;
                storage[i, 1] = p1 * y2;
                storage[i, 2] = p1;
                storage[i, 3] = p2*y1;
                storage[i, 4] = p2 * y2;
                storage[i, 5] = p2;
                storage[i, 6] = y1;
                storage[i, 7] = y2;
                storage[i, 8] = 1;
            }
            
            var m1 = new GMatrix(storage);
            double max = 1;
            double min = -1;
            for(var i = 0; i < m1.rows;i ++)
            {
                for (var j = 0; j < m1.cols; j++)
                {
                    var v = storage[i, j];
                    if (v > max) max = v;
                    if (v < min) min = v;
                }
            }
            var scal = Math.Max(Math.Abs(min), max);
            for (var i = 0; i < m1.rows; i++)
            {
                for (var j = 0; j < m1.cols; j++)
                {
                    var v = storage[i, j];
                    storage[i, j] = v / scal;
                }
            }
            var resultM = m1.tranpose().cross(m1);
            Console.WriteLine(resultM);
        }

        public void Solve(GMatrix m)
        {
            MaxApply(m, 0);
        }
        static void MaxApply(GMatrix m, int pos)
        {
            double max = 0;
            for (var i = 0; i < m.rows;i++)
            {
                var v = Math.Abs(m.storage[i, pos]);
                if (v > max) max = v;
            }
            for (var i = 0; i < m.rows; i++)
            {
                var v = m.storage[i, pos];
                if (v == 0) continue;
                var scal = max / v;
                for (var j = pos; j < m.cols; j++)
                {
                    m.storage[i, j]*=scal;
                }

            }
        }
    }

    public class GMatrix
    {
        public double[,] storage { get; protected set; }
        public GMatrix(double[,] v)
        {
            storage = v;
        }
        public GMatrix tranpose()
        {
            var r = rows;
            var c = cols;
            var newStorage = new double[c,r];
            for (var i = 0; i < r; i++)
            {
                for(var j = 0; j < c;j++)
                {
                    newStorage[j, i] = storage[i, j];
                }
            }
            return new GMatrix(newStorage);
        }
        public int cols { get { return storage.GetLength(1);  } }
        public int rows { get { return storage.GetLength(0); } }

        public GMatrix cross(GMatrix m)
        {
            var r = rows;
            var mc = m.cols;
            if (r != mc) throw new InvalidOperationException($"Cross: row {r} and col {mc} must equal");
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
                        total += storage[i, k] * m.storage[k, j];
                    }
                    newStorage[i, j] = total;
                }
            }
            return new GMatrix(newStorage);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < cols;j++)
                {
                    if (j > 0) sb.Append(",");
                    sb.Append(storage[i, j]);                    
                }
                sb.Append("\n");
            }
            return sb.ToString();
        }
    }
}
