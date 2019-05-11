﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace MatrixTest
{
    class Program
    {
        const string saveFilePath = @"C:\test\netCvReco\data\";
        const string saveFileName_corners = saveFilePath + "corners.txt";
        static void Main(string[] args)
        {
            //var r = new GMatrix(new double[,] { { 1, 2 }, { 3, 4 } , { 1, 1 } }).cross(new GMatrix(new double[,] { { 1, 1 ,1}, { 3, 4 ,1} }));
            //Console.WriteLine(r);
            //new Calib().Calc(new PointF[] {
            //    new Point(1,2),
            //    new Point(3,4),
            //    new Point(5,4),
            //    new Point(6,4),
            //    new Point(7,4),
            //    new Point(8,4),
            //    new Point(9,4),
            //    new Point(10,4),
            //    new Point(11,4),
            //    new Point(12,4),
            //    new Point(13,4),
            //    new Point(14,4),
            //},
            //new PointF[] {
            //    new Point(1,2),
            //    new Point(3,4),
            //    new Point(3,4),
            //    new Point(3,4),
            //    new Point(3,4),
            //    new Point(3,4),
            //    new Point(3,4),
            //    new Point(3,4),
            //    new Point(3,4),
            //    new Point(3,4),
            //    new Point(3,4),
            //    new Point(3,4),
            //}
            //);


            var lines = File.ReadAllLines(saveFileName_corners);
            var resa = stringToCorner(lines);
            var res = Calib.Calc(resa[0].SelectMany(x=>x).ToArray(), resa[1].SelectMany(x=>x).ToArray());

            

            Console.WriteLine(res);
        }

        static string cornerToString(PointF[][] corner)
        {
            var sb = new StringBuilder();
            sb.Append(corner.Length).Append("\r\n");
            for (var i = 0; i < corner.Length; i++)
            {
                var ci = corner[i];
                for (var j = 0; j < ci.Length; j++)
                {
                    sb.Append(ci[j].X).Append(",").Append(ci[j].Y).Append(" ");
                }
                sb.Append("\r\n");
            }
            return sb.ToString();
        }
        public static List<PointF[][]> stringToCorner(string[] lines)
        {
            var res = new List<PointF[][]>();
            while (lines.Length > 0)
            {
                int len = Convert.ToInt32(lines[0]);
                var curLines = new PointF[len][];
                res.Add(curLines);
                for (int i = 0; i < len; i++)
                {
                    var line = lines[i + 1];
                    var segs = line.Split(' ');
                    var pts = new List<PointF>();
                    foreach (var seg in segs)
                    {
                        if (seg.Trim() == "") continue;
                        var ps = seg.Split(',');
                        pts.Add(new PointF(Convert.ToSingle(ps[0]), Convert.ToSingle(ps[1])));
                    }
                    curLines[i] = pts.ToArray();
                }
                lines = lines.Skip(len + 1).ToArray();
            }
            return res;
        }


    }


    public class Calib
    {
        public static GMatrix Calc(PointF[] points1, PointF[] points2)
        {
            var m1 = new GMatrix(points1.Length, 9);
            for (var i = 0; i < points1.Length; i++)
            {
                var pp1 = points1[i];
                var pp2 = points2[i];
                var y1 = pp1.X;
                var y2 = pp1.Y;
                var p1 = pp2.X;
                var p2 = pp2.Y;
                var cur = m1.storage[i];
                cur[0] = y1 * p1;
                cur[1] = p1 * y2;
                cur[2] = p1;
                cur[3] = p2*y1;
                cur[4] = p2 * y2;
                cur[5] = p2;
                cur[6] = y1;
                cur[7] = y2;
                cur[8] = 1;
            }
                        
            double max = 1;
            double min = -1;
            for(var i = 0; i < m1.rows;i ++)
            {
                for (var j = 0; j < m1.cols; j++)
                {
                    var v = m1.storage[i][j];
                    if (v > max) max = v;
                    if (v < min) min = v;
                }
            }
            var scal = Math.Max(Math.Abs(min), max);
            for (var i = 0; i < m1.rows; i++)
            {
                for (var j = 0; j < m1.cols; j++)
                {
                    var v = m1.storage[i][j];
                    m1.storage[i][j] = v / scal;
                }
            }
            var svdA = svd.SVD(m1);
            var Fhat = new GMatrix(3, 3);
            int at = 0;
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    Fhat.storage[i][j] = svdA.v[at++, 8];
                }
            }

            var FhatSvd = svd.SVD(Fhat);
            var mm = FhatSvd.q.Select(x => Math.Abs(x)).Min();
            var d = new double[3, 3];
            d[0, 0] = FhatSvd.q[0];
            d[1, 1] = FhatSvd.q[1];
            d[2, 2] = FhatSvd.q[2];
            for (var i =0; i < 3; i++)
            {
                if (Math.Abs(d[i, i]) == mm) d[i, i] = 0;
            }
            var res = new GMatrix(FhatSvd.u).cross(new GMatrix(d)).cross(new GMatrix(FhatSvd.v).tranpose());
            return res;
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
                var v = Math.Abs(m.storage[i][pos]);
                if (v > max) max = v;
            }
            for (var i = 0; i < m.rows; i++)
            {
                var v = m.storage[i][pos];
                if (v == 0) continue;
                var scal = max / v;
                for (var j = pos; j < m.cols; j++)
                {
                    m.storage[i][j]*=scal;
                }

            }
        }
    }

    public class GMatrix
    {
        public double[][] storage { get; protected set; }
        public GMatrix(int r, int c)
        {
            rows = r;
            cols = c;
            init();
        }
        public GMatrix(double[,] v)
        {            
            rows = v.GetLength(0);
            cols = v.GetLength(1);
            init();
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c<cols;c++)
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
            var newStorage = new GMatrix(c,r);
            for (var i = 0; i < r; i++)
            {
                for(var j = 0; j < c;j++)
                {
                    newStorage.storage[j][i] = storage[i][j];
                }
            }
            return newStorage;
        }
        public int cols { get; protected set; }
        public int rows { get; protected set; }

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
                        total += storage[i][k] * m.storage[k][j];
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
                    sb.Append(storage[i][j].ToString("0.00000"));                    
                }
                sb.Append("\n");
            }
            return sb.ToString();
        }
    }
}
