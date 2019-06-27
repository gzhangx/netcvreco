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

        public static void FindEpipole(PointFloat[] pts, PointSide side, GMatrix f)
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
            Console.WriteLine($"{res[0].ToString("0.00")},{res[1].ToString("0.00")},{res[2].ToString("0.00")}");
        }
        
        private static GMatrix GetEpLineABC(PointFloat pts, PointSide side, GMatrix f)
        {
            if (side == PointSide.Right)
            {
                f = f.tranpose();
            }
            var gm = f.dot(new GMatrix(new double[3, 1] { { pts.X }, { pts.Y }, { 1 } }));
            //var ms = gm.storage;
            //var a = ms[0][0];
            //var b = ms[1][0];
            //var c = ms[2][0];
            return gm;
        }
    }
}
