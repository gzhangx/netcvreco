using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netCvLib
{
    public interface PreVidStream
    {
        int Total { get; }
        int Pos { get; set; }
        Mat GetCurMat();
    }
    public class VidLoc
    {
        public class DiffLoc
        {
            public int Pos { get; set; }
            public double diff { get; set; }
            public List<DiffVect> posVet { get; set; }
            public DiffVector vect
            {
                get
                {
                    return ShiftVecProcessor.calculateTotalVect(posVet);
                }
            }
        }
        
        public static DiffLoc FindInRage(PreVidStream stream, Mat curr, int steping = 10, int from = 0, int to = 0)
        {
            if (to == 0 || to > stream.Total) to = stream.Total;
            if (from < 0) from = 0;
            DiffLoc curMax = null;
            for (int pos = from; pos < to; pos += steping)
            {
                stream.Pos = pos;
                var processor = new ShiftVecProcessor(curr, stream.GetCurMat());
                var all = processor.GetAllDiffVect();
                var maxGood = all.Average(a => a.Diff);
                if (curMax == null) {
                    curMax = new VidLoc.DiffLoc
                    {
                        diff = maxGood,
                        Pos = pos,
                        posVet = all,
                    };
                }
                else
                {
                    if (maxGood > curMax.diff)
                    {
                        curMax = new DiffLoc
                        {
                            diff = maxGood,
                            Pos = pos,
                            posVet = all,
                        };
                    }
                }

            }
            Console.WriteLine($"max at {curMax.Pos} {curMax.diff.ToString("0.00")}");
            if (steping == 1) return curMax;
            return FindInRage(stream, curr, 1, curMax.Pos - steping, curMax.Pos + steping);
        }


        public class RealTimeTrackLoc
        {
            public int CurPos { get; set; }  //input
            public int NextPos { get; set; }//output
            public DiffVector vect { get; set; } //output
            public int LookAfter = 5;
        }


        public static void FindObjectDown(PreVidStream stream, Mat curr, RealTimeTrackLoc prms)
        {
            int from = prms.CurPos + 1;
            int to = from + prms.LookAfter;
            if (to == 0 || to > stream.Total) to = stream.Total;
            if (from < 0) from = 0;
            DiffLoc curMax = null;
            List<DiffLoc> processed = new List<DiffLoc>();
            //double dxT = 0, dyT = 0;
            //int numD = 0;
            for (int pos = from; pos < to; pos ++)
            {
                var loc = FindInRage(stream, curr, 1, pos, pos + 1);
                processed.Add(loc);
                if (curMax == null) curMax = loc;
                else
                {
                    if (curMax.diff < loc.diff)
                    {
                        curMax = loc;
                    }
                }
                //stream.Pos = pos;
                //var processor = new ShiftVecProcessor(curr, stream.GetCurMat());
                //var all = processor.GetAllDiffVect();
                //var maxGood = all.Average(a => a.Diff);
                //if (curMax == null)
                //{
                //    curMax = new VidLoc.DiffLoc
                //    {
                //        diff = maxGood,
                //        Pos = pos,
                //    };
                //}
                //else
                //{
                //    if (maxGood > curMax.diff)
                //    {
                //        curMax = new DiffLoc
                //        {
                //            diff = maxGood,
                //            Pos = pos,
                //        };
                //    }
                //}
                //var thisRes = ShiftVecProcessor.calculateTotalVect(all);
                //dxT += thisRes.X;
                //dyT += thisRes.Y;
                //numD++;
            }
            Console.WriteLine($"max at {curMax.Pos} {curMax.diff.ToString("0.00")}");
            prms.NextPos = curMax.Pos;
            bool found = false;
            DiffLoc nextMax = null;
            foreach(var l in processed)
            {
                if(found)
                {
                    nextMax = l;
                    break;
                }
                if (l == curMax) found = true;
            }
            if (nextMax != null)
            {
                prms.vect = nextMax.vect;
            } else
            {
                nextMax = processed.Last();
                prms.vect = nextMax.vect;
            }
            //DiffVector resVect = null;
            //if (numD == 0) {
            //    resVect = new DiffVector(0, 0);
            //}
            //else
            //{
            //    resVect = new DiffVector(dxT / numD, dyT / numD);
            //}
            //prms.vect = resVect;
        }
    }
}
