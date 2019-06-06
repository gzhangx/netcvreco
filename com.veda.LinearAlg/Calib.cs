using System;


namespace com.veda.LinearAlg
{
    public class PointFloat
    {
        public PointFloat(float x, float y)
        {
            X = x;
            Y = y;
        }
        public float X { get; protected set; }
        public float Y { get; protected set; }
    }
    public class Calib
    {
        public static GMatrix SolveSvd(GMatrix m1)
        {
            double max = 1;
            double min = -1;
            for (var i = 0; i < m1.rows; i++)
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
            var svdA = JacobSvd.JacobiSVD(m1);
            var Fhat = new GMatrix(3, 3);
            int at = 0;
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    Fhat.storage[i][j] = svdA.Vt.storage[at++][8];
                }
            }
            return Fhat;
        }
        public static GMatrix Calc(PointFloat[] points1, PointFloat[] points2)
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
                cur[3] = p2 * y1;
                cur[4] = p2 * y2;
                cur[5] = p2;
                cur[6] = y1;
                cur[7] = y2;
                cur[8] = 1;
            }
            
            var Fhat = SolveSvd(m1);

            var FhatSvd = JacobSvd.JacobiSVD(Fhat);
            var d = FhatSvd.getWMat();
            d.storage[2][2] = 0;

            var res = FhatSvd.U.dot(d).dot(FhatSvd.Vt);
            return res;
        }

        public void Solve(GMatrix m)
        {
            MaxApply(m, 0);
        }
        static void MaxApply(GMatrix m, int pos)
        {
            double max = 0;
            for (var i = 0; i < m.rows; i++)
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
                    m.storage[i][j] *= scal;
                }

            }
        }
    }
}
