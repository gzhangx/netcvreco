using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.veda.LinearAlg
{
    public class CalibRect
    {
        public enum PointSide
        {
            Left,
            Right
        }

        public static PointFloat FindEpipole(PointFloat[] pts, PointSide side, GMatrix f)
        {
            if (side == PointSide.Right)
            {
                f = f.tranpose();
            }

            GMatrix lines = new GMatrix(pts.Length, 3);
            var storage = lines.storage;
            for (int i = 0; i < pts.Length; i++)
            {
                var cur = storage[i];
                var pt = pts[i];
                var gm = f.dot(new GMatrix(new double[3, 1] { { pt.X }, { pt.Y }, { 1 } }));
                var ms = gm.storage;
                for (int j = 0; j < 3; j++)
                    cur[j] = ms[j][0];                               
            }

            var svdA = JacobSvd.JacobiSVD(lines);
            var res = svdA.Vt.storage[2];
            //Console.WriteLine($"{res[0].ToString("0.00")},{res[1].ToString("0.00")},{res[2].ToString("0.0000000")}");
            //Console.WriteLine($"{(res[0]/res[2]).ToString("0.00")},{(res[1]/res[2]).ToString("0.00")}");
            return new PointFloat((float)(res[0] / res[2]), (float)(res[1] / res[2]));
        }

        public static GMatrix GetH2(PointFloat e, PointFloat imgSize)
        {
            var w2 = -imgSize.X / 2;
            var h2 = -imgSize.Y / 2;
            GMatrix T = new GMatrix(new double[,] {
                {1,0, w2 },
                 {0,1, h2 },
                 {0,0,1 }
            });
            var e1 = e.X + w2;
            var e2 = e.Y + h2;
            var l = Math.Sqrt((e1 * e1) + (e2 * e2));
            var alph = e1 >= 0 ? 1 : -1;
            GMatrix R = new GMatrix(new double[,] {
                { alph*e1/l, alph*e2/l, 0},
                 { -1*alph*e2/l, alph*e1/l, 0 },
                 {0,0,1 }
            });
            GMatrix G = new GMatrix(new double[,]
            {
                {1,0,0 },
                {0,1,0 },
                {-1/e1,0,1 }
            });

            return GMatrix.Inverse3x3(T).dot(G.dot(R.dot(T)));
        }


        static PointFloat[] perspectiveTransform(PointFloat[] pts, GMatrix mat)
        {
            List<PointFloat> res = new List<PointFloat>();
            foreach( var pt in pts)
            {
                var npt = mat.dot(pt.ToVect());
                var w = npt.storage[2][0];
                res.Add(new PointFloat(npt.storage[0][0] / w, npt.storage[1][0] / w));
            }
            return res.ToArray();
        }
        public static GMatrix GetH1(PointFloat e, PointFloat imgSize, GMatrix F, GMatrix H2,
            PointFloat[] leftPts, PointFloat[] rightPts)
        {
            var ex = new GMatrix(new double[,]
            {
                { 0, -1, e.Y },
                { 1, 0, -e.X },
                { -e.Y, e.X, 0}
            });
            var e_111 = new GMatrix(new double[,]
            {
                { e.X, e.X, e.X},
                { e.Y, e.Y, e.Y},
                { 1, 1, 1},
            });
            var m = ex.dot(F).add(e_111);

            var h0h = H2.dot(m);

            var m1 = perspectiveTransform(leftPts, h0h);
            var m2 = perspectiveTransform(rightPts, H2);
            var abuf = new double[leftPts.Length, 3];
            for (var i = 0; i < leftPts.Length; i++)
            {
                var pt = leftPts[i];
                abuf[i, 0] = pt.X;
                abuf[i, 1] = pt.Y;
                abuf[i, 2] = 1;
            }
            var A = new GMatrix(abuf);
            var svcr = JacobSvd.JacobiSVD(A);
            
            double[] B = new double[rightPts.Length];
            for (var i = 0; i < rightPts.Length; i++) B[i] = m2[i].X;
            var x = Util.SVBkSb(svcr.U, svcr.W, svcr.Vt, B);

            var Ha = new GMatrix(new double[,]
            {
                {x[0], x[1], x[2] },
                {0,1,0 },
                {0,0,1 }
            });

            var res = Ha.dot(h0h);
            return res;
        }


        public class RectifyResult
        {
            public GMatrix H1;
            public GMatrix H2;
            public GMatrix F;

            public PointFloat el;
            public GMatrix LeftIntrinics;
            public GMatrix RightIntrinics;
        }

        public class StereoPoints
        {
            public PointFloat[] Left;
            public PointFloat[] Right;
        }

        public static RectifyResult Rectify(List<StereoPoints> allPts, PointFloat imgSize, int CalibGridRow=6, int CalibGridCol = 3)
        {
            var leftPts = allPts.SelectMany(x => x.Left).ToArray();
            var rightPts = allPts.SelectMany(x => x.Right).ToArray();
            var F = Calib.CalcFundm(leftPts, rightPts);
            var epol = CalibRect.FindEpipole(leftPts, PointSide.Left, F);


            var h2 = CalibRect.GetH2(epol, new PointFloat(imgSize.X, imgSize.Y));
            var h1 = CalibRect.GetH1(epol, new PointFloat(imgSize.X, imgSize.Y), F, h2, allPts[0].Left, allPts[0].Right);


            Func<Func<StereoPoints, PointFloat[]>, PointFloat[][]> fetch = f =>
             {
                 PointFloat[][] res = new PointFloat[allPts.Count][];
                 for(var i = 0; i < allPts.Count; i++)
                 {
                     res[i] = f(allPts[i]);
                 }
                 return res;
             };
            return new RectifyResult
            {
                H1 = h1,
                H2 = h2,
                F = F,
                el = epol,
                LeftIntrinics = Calib.EstimateIntranics(fetch(x=>x.Left), CalibGridRow, CalibGridCol),
                RightIntrinics = Calib.EstimateIntranics(fetch(x => x.Right), CalibGridRow, CalibGridCol),
            };
        }
    }
}
