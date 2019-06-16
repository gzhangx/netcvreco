using System;
using System.Linq;


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
        public GMatrix ToVect()
        {
            return new GMatrix(new double[,] { { X }, { Y }, { 1 } });
        }
    }
    public class Calib
    {
        public class TandTi
        {
            public GMatrix t;
            public GMatrix ti;
        }
        public static GMatrix SolveSvd3x3(GMatrix m1)
        {
            JacobSvd.SvdRes svdA = SolveSvd(m1);
            var Fhat = new GMatrix(3, 3);
            int at = 0;
            var ms = svdA.Vt.storage[8];
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    Fhat.storage[i][j] = ms[at++];
                }
            }
            return Fhat;
        }

        protected static double getMean(PointFloat[] pts, Func<PointFloat,double>get)
        {
            var total = pts.Sum(get);
            return total / pts.Length;
        }
        protected static double getVariance(PointFloat[] pts, Func<PointFloat, double> get)
        {
            var mean = getMean(pts, get);
            return pts.Sum(p =>
            {
                var v = get(p) - mean;
                return (v * v);
            })/pts.Length;
        }
        private static JacobSvd.SvdRes SolveSvd(GMatrix m1)
        {
            //double max = 1;
            //double min = -1;
            //for (var i = 0; i < m1.rows; i++)
            //{
            //    for (var j = 0; j < m1.cols; j++)
            //    {
            //        var v = m1.storage[i][j];
            //        if (v > max) max = v;
            //        if (v < min) min = v;
            //    }
            //}
            //var scal = Math.Max(Math.Abs(min), max);
            //for (var i = 0; i < m1.rows; i++)
            //{
            //    for (var j = 0; j < m1.cols; j++)
            //    {
            //        var v = m1.storage[i][j];
            //        m1.storage[i][j] = v / scal;
            //    }
            //}
            var svdA = JacobSvd.JacobiSVD(m1);
            return svdA;
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
            
            var Fhat = SolveSvd3x3(m1);

            var FhatSvd = JacobSvd.JacobiSVD(Fhat);
            var d = FhatSvd.getWMat();
            d.storage[2][2] = 0;

            var res = FhatSvd.U.dot(d).dot(FhatSvd.Vt);
            return res;
        }

        static TandTi GetNormalizedMatrix(PointFloat[] pts)
        {
            var xmean = getMean(pts, p => p.X);
            var ymean = getMean(pts, p => p.Y);
            var xvar = getVariance(pts, p => p.X);
            var yvar = getVariance(pts, p => p.Y);

            var xs = Math.Sqrt(2 / xvar);
            var ys = Math.Sqrt(2 / yvar);

            var m = new GMatrix(new double[,]
            {
                { xs, 0, -xs*xmean },
                { 0, ys, -ys * ymean },
                { 0, 0, 1 }
            });
            var mi = new GMatrix(new double[,] {
                {1/xs ,  0 , xmean },
                { 0, 1/ ys, ymean},
                { 0,0,1}
            });
            return new TandTi
            {
                t = m,
                ti = mi,
            };
        }
        public static GMatrix EstimateHomography(PointFloat[] points, int w = 6, int h = 3)
        {
            var pos = new PointFloat[w * h];
            int at = 0;
            for (var j = 0; j < h; j++) 
            {
                for (var i = 0; i < w; i++)
                {
                    pos[at++] = new PointFloat(i , j );
                }
            }
            return EstimateHomography(points, pos);
        }
        public static GMatrix EstimateHomography(PointFloat[] points, PointFloat[] checkBoardLoc)
        {
            var nu = GetNormalizedMatrix(points);
            var nx = GetNormalizedMatrix(checkBoardLoc);
            var m1 = new GMatrix(points.Length*2, 9);
            for (var i = 0; i < points.Length; i++)
            {
                var pts = nu.t.dot(points[i].ToVect()).noramByLast();
                var obj = nx.t.dot(checkBoardLoc[i].ToVect()).noramByLast();
                var ii = i * 2;
                //var xy = points[i];
                var x = pts.storage[0][0];
                var y = pts.storage[1][0];
                var X = obj.storage[0][0];
                var Y = obj.storage[1][0];
                var cur = m1.storage[ii];
                cur[0] = -X;
                cur[1] = -Y;
                cur[2] = -1;
                cur[3] = 0;
                cur[4] = 0;
                cur[5] = 0;
                cur[6] = x*X;
                cur[7] = x*Y;
                cur[8] = x;

                cur = m1.storage[ii+1];
                cur[0] = 0;
                cur[1] = 0;
                cur[2] = 0;
                cur[3] = -X;
                cur[4] = -Y;
                cur[5] = -1;
                cur[6] = y * X;
                cur[7] = y * Y;
                cur[8] = y;
            }

            var res = SolveSvd3x3(m1);

            var denormed = nu.ti.dot(res).dot(nx.t).noramByLast();
            
            return denormed;
        }

        public static void EstimateIntranics(PointFloat[] points, int w = 6, int h = 3)
        {
            /*
             *   hij
             *            h12  h22  h32
             *            
             *   h11      b11  b12  b13
             *   h21      b12  b22  b23
             *   h31      b13  b23  b33
             * 
             * 
             *   h11 h12 t1
             *   h21 h22 t2
             *   h31 h32 t3
             * 
             */
            var homo = EstimateHomography(points, w, h);
            Action<int, int, double[]> FillV = (i, j, cur) =>
              {
                  Func<int, int, double> hval = (hcol, hrow) =>
                  {
                      return homo.storage[hrow][hcol];
                  };
                  cur[0] = hval(0, i) * hval(0, j);
                  cur[1] = (hval(0, i) * hval(1, j)) + (hval(1, i) * hval(0, j));
                  cur[2] = (hval(2, i) * hval(0, j)) + (hval(0, i) * hval(2, j));
                  cur[3] = hval(1, i) * hval(1, j);
                  cur[4] = (hval(2, i) * hval(1, j)) + (hval(1, i) * hval(2, j));
                  cur[5] = hval(2, i) * hval(2, j);
              };

            GMatrix m = new GMatrix(points.Length*2, 6);
            for (var i = 0; i < points.Length; i+=2)
            {
                FillV(0, 1, m.storage[i]);
                var v00 = new double[6];
                FillV(0, 0, v00);
                var v11 = new double[6];
                FillV(1, 1, v11);
                var r2 = m.storage[i + 1];
                for (var j = 0; j < 6; j++)
                {
                    r2[j] = v00[j] - v11[j];
                }
            }
            var svdr = SolveSvd(m);

            var a = new GMatrix(3, 3);
            var vtl = svdr.Vt.storage[5];
            Func<int, double> gb = i=> vtl[i];
            var gb0_4 = gb(0) * gb(4);
            var gb0_2 = gb(0) * gb(2);
            var gb1x1 = gb(1) * gb(1);
            var vc = ((gb(1) * gb(3)) - gb0_4) / (gb0_2 - gb1x1);
            var l = gb(5) - ((gb(3)*gb(3) + vc*(gb(1)*gb(2)-gb0_4)) / gb(0));
            var alpha = Math.Sqrt(l / gb(0));
            var beta = Math.Sqrt((l * gb(0)) / (gb0_2 - gb1x1));
            var gamma = -1 * (gb(1) * alpha * alpha) * beta / l;
            var uc = gamma * vc / beta - (gb(3) * alpha * alpha / l);
            a.storage[0][0] = alpha;
            a.storage[1][1] = beta;
            a.storage[0][1] = gamma;
            a.storage[0][2] = uc;
            a.storage[1][2] = vc;
            a.storage[2][2] = 1;
            Console.WriteLine(a);
            return;
             var b = new GMatrix(3, 3);
            b.storage[0][0] = svdr.Vt.storage[0][5];
            b.storage[0][1] = b.storage[1][0] = svdr.Vt.storage[1][5]; //b12
            b.storage[0][2] = b.storage[2][0] = svdr.Vt.storage[2][5]; //b13
            b.storage[1][1] = svdr.Vt.storage[3][5]; //b22
            b.storage[1][2] = b.storage[2][1] = svdr.Vt.storage[4][5]; //b23
            b.storage[2][2] = svdr.Vt.storage[5][5]; //b33
            //b= K-t*K-1
            Console.WriteLine(b);
            var A1 = Cholesky3x3(b);
            Console.WriteLine(Inverse3x3(A1));
        }

        protected static GMatrix Cholesky3x3(GMatrix m)
        {
            var l11 = Math.Sqrt(m.storage[0][0]);
            var l21 = m.storage[0][1] / l11;
            var l31 = m.storage[0][2] / l11;
            var l22 = Math.Sqrt(m.storage[1][1] - (l21 * l21));
            var l32 = (m.storage[1][2] - (l31 * l21)) / l22;
            var l33 = Math.Sqrt(m.storage[2][2] - ((l31*l31) + (l32*l32)));
            GMatrix rm = new GMatrix(3, 3);
            rm.storage[0][0] = l11;
            rm.storage[0][1] = l21;
            rm.storage[0][2] = l31;
            rm.storage[1][1] = l22;
            rm.storage[1][2] = l32;
            rm.storage[2][2] = l33;
            return rm;
        }

        protected static GMatrix Inverse3x3(GMatrix m)
        {
            double det = 0;
            var mat = m.storage;
            for (var i = 0; i < 3; i++)
                det = det + (mat[0][i] * (mat[1][(i + 1) % 3] * mat[2][(i + 2) % 3] - mat[1][(i + 2) % 3] * mat[2][(i + 1) % 3]));


            var res = new GMatrix(3, 3);
            for (var i = 0; i < 3; ++i)
            {
                for (var j = 0; j < 3; ++j)
                    res.storage[i][j] = ((mat[(j + 1) % 3][(i + 1) % 3] * mat[(j + 2) % 3][(i + 2) % 3]) - (mat[(j + 1) % 3][(i + 2) % 3] * mat[(j + 2) % 3][(i + 1) % 3])) / det;
            }
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
