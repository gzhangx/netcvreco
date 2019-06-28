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

    }
}
