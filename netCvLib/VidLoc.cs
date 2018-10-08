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
        List<DiffVector> Vectors { get; }
    }

    public class VidLoc
    {
        
        public static DiffVect CompDiff(Mat input, Mat comp, BreakDiffDebugReporter reporter)
        {
            var processor = new ShiftVecProcessor(input, comp);
            var vect = processor.GetAllDiffVect();            
            if (reporter!= null) reporter.ReportStepChanges(processor, vect);
            return vect;
        }

        public static DiffVect FindInRage(PreVidStream stream, Mat curr, int steping = 1, int from = 0, int to = 0)
        {
            if (to == 0 || to > stream.Total) to = stream.Total;
            if (from < 0) from = 0;
            DiffVect curMax = null;
            for (int pos = from; pos < to; pos += steping)
            {
                stream.Pos = pos;
                var processor = new ShiftVecProcessor(curr, stream.GetCurMat());
                var all = processor.GetAllDiffVect();
                all.VidPos = pos;
                var maxGood = all.Vector.Diff;
                if (curMax == null) {
                    curMax = all;
                }
                else
                {
                    if (maxGood > curMax.Vector.Diff)
                    {
                        curMax = all;
                    }
                }

            }
            //Console.WriteLine($"max at {curMax.Pos} {curMax.diff.ToString("0.00")}");
            if (steping == 1) return curMax;
            return FindInRage(stream, curr, 1, curMax.VidPos - steping, curMax.VidPos + steping);
        }


        const int LOOKAFTER = 5;
        public class RealTimeTrackLoc
        {
            public int CurPos { get; set; }  //input
            public int EndPos { get; set; }
            public bool ShouldStop()
            {
                return CurPos + 5 >= EndPos;
            }
            public int NextPos { get; set; }//output
            public DiffVector vect { get; set; } //output
            public DiffVect diffVect { get; set; } //debug output, difference to current
            public DiffVector nextVect { get; set; } //debutoutput, what next frame should go
            public List<DiffVect> DebugAllLooks { get; set; }
            public int LookAfter = LOOKAFTER;
            public void LookAfterReset()
            {
                LookAfter = LOOKAFTER;
            }
            //public bool notFound = false;
            public double diff { get; set; }
            public void LongLook()
            {
                CurPos -= 10;
                if (CurPos < 0) CurPos = 0;
                LookAfter = CurPos + 15;
            }
        }


        public static List<DiffVect> SortProcessDiffVects(List<DiffVect> processed)
        {
            const double SPREADLIMIT = 0.70;
            var ordered = processed.OrderByDescending(x => x.Vector.Diff).ToList();
            var spreadThreadshold = processed[0].Vector.Diff* SPREADLIMIT;
            return ordered.TakeWhile(x => x.Vector.Diff >= spreadThreadshold).OrderBy(x=>Math.Abs(x.Vector.X) + Math.Abs(x.Vector.Y)).ToList();
        }
        public static void FindObjectDown(PreVidStream stream, Mat curr, RealTimeTrackLoc prms, BreakDiffDebugReporter reporter)
        {
            int from = prms.CurPos;
            int to = from + prms.LookAfter;
            if (to == 0 || to > stream.Total) to = stream.Total;
            if (from < 0) from = 0;
            
            List<DiffVect> processed = new List<DiffVect>();
            //double dxT = 0, dyT = 0;
            //int numD = 0;
            for (int pos = from; pos < to; pos ++)
            {
                var loc = FindInRage(stream, curr, 1, pos, pos + 1);
                processed.Add(loc);                
            }

            processed.OrderByDescending(x => x.Vector.Diff).Take(3);

            prms.DebugAllLooks = processed;
            var sorted = SortProcessDiffVects(processed);
            var curMax = sorted.FirstOrDefault();
            if (curMax == null || curMax.VidPos >= stream.Total - 1)
            {
                Console.WriteLine($"max not found from={from} to={to} total={stream.Total}");
                return;
            }
            //Console.WriteLine($"max at {curMax.Pos} {curMax.diff.ToString("0.00")}");
            prms.NextPos = curMax.VidPos;
            prms.diff = curMax.Vector.Diff;
            prms.vect = curMax.Vector;

            stream.Pos = curMax.VidPos;
            var diff = CompDiff(curr, stream.GetCurMat(), reporter);
            var nextVect = stream.Vectors[curMax.VidPos];            
            //diff: negative if need to turn left
            //vect: positive if need to turn left
            prms.vect = new DiffVector(nextVect.X + diff.Vector.X, nextVect.Y + diff.Vector.Y, diff.Vector.Diff);

            reporter.InfoReport($"===> {(prms.vect.X>0?"L":"R")} ({prms.vect}) nextX {nextVect.X} diffX {diff.Vector.X} pos {curMax.VidPos}");

            prms.diffVect = diff;
            prms.nextVect = nextVect;
        }



        public static  void CamTracking(Mat curImg, VidLoc.RealTimeTrackLoc realTimeTrack, PreVidStream vidProvider, IDriver driver, BreakDiffDebugReporter debugReporter)
        {
            //realTimeTrack.CurPos = image1Ind;
            realTimeTrack.LookAfterReset();
            if (!realTimeTrack.ShouldStop())
            {
                int origImageInd = realTimeTrack.CurPos;
                debugReporter.ReportInProcessing(true);
                VidLoc.FindObjectDown(vidProvider, curImg, realTimeTrack, debugReporter);
                debugReporter.ReportInProcessing(false);

                //var lookBackCount = 0;
                //while (realTimeTrack.diff < 0.5 && lookBackCount < 3)
                //{
                //    driver.Stop();
                //    realTimeTrack.LongLook();
                //    VidLoc.FindObjectDown(vidProvider, curImg, realTimeTrack, debugReporter);
                //    //info.Text = text = $"Tracked vid at ${image1Ind} cam at ${image2Ind} next point ${realTimeTrack.NextPos} ${realTimeTrack.vect}  ===> diff {realTimeTrack.diff} LB {lookBackCount}";
                //    //Console.WriteLine(text);
                //    lookBackCount++;
                //}

                vidProvider.Pos = origImageInd;
            }
            driver.Track(realTimeTrack);
            //if (debugReporter.DebugMode)
            //{                
                //Mat m1 = vidProvider.GetCurMat();
                //breakAndDiff(m1, curImg, debugReporter);                
            //}
        }        

        public static void breakAndDiff(Mat m1, Mat m2, BreakDiffDebugReporter reporter)
        {
            var curProcessor = new ShiftVecProcessor(m1, m2);
            //Mat res = ShiftVecDector.BreakAndNearMatches(m1, m2);
            var vect = curProcessor.GetAllDiffVect();                     


            Mat res = curProcessor.ShowAllStepChange(vect);
            reporter.Report(res, vect);
            
        }
    }

    public interface BreakDiffDebugReporter
    {
        bool DebugMode { get; }
        void Report(Mat res,DiffVect vect);
        void ReportStepChanges(ShiftVecProcessor proc, DiffVect vect);
        void InfoReport(string info);
        void ReportInProcessing(bool processing);
    }
}
