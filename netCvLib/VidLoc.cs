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
        List<DiffVectorWithDiff> Vectors { get; }
    }

    public class DiffVectorWithDiff
    {
        public DiffVector Vector { get; set; }
        public double Diff { get; set; }
        public List<DiffVect> DebugVectors { get; set; }
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
        
        public static DiffVectorWithDiff CompDiff(Mat input, Mat comp, bool returnAllVects = false)
        {
            var processor = new ShiftVecProcessor(input, comp);
            var all = processor.GetAllDiffVect();
            var averageDiff = all.Average(a => a.Diff);
            var vect = ShiftVecProcessor.calculateTotalVect(all);
            return new DiffVectorWithDiff
            {                
                Diff = averageDiff,
                Vector = vect,
                DebugVectors = returnAllVects? all:null,
            };
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
            //Console.WriteLine($"max at {curMax.Pos} {curMax.diff.ToString("0.00")}");
            if (steping == 1) return curMax;
            return FindInRage(stream, curr, 1, curMax.Pos - steping, curMax.Pos + steping);
        }


        public class RealTimeTrackLoc
        {
            public int CurPos { get; set; }  //input
            public int NextPos { get; set; }//output
            public DiffVector vect { get; set; } //output
            public DiffVector diffVect { get; set; } //debug output, difference to current
            public DiffVector nextVect { get; set; } //debutoutput, what next frame should go
            public int LookAfter = 5;
            //public bool notFound = false;
            public double diff { get; set; }
            public void LongLook()
            {
                CurPos -= 10;
                if (CurPos < 0) CurPos = 0;
                LookAfter = CurPos + 15;
            }
        }


        public static void FindObjectDown(PreVidStream stream, Mat curr, RealTimeTrackLoc prms, BreakDiffDebugReporter reporter)
        {
            int from = prms.CurPos;
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
            }
            if (curMax == null || curMax.Pos >= stream.Total - 1)
            {
                Console.WriteLine($"max not found from={from} to={to} total={stream.Total}");
                return;
            }
            //Console.WriteLine($"max at {curMax.Pos} {curMax.diff.ToString("0.00")}");
            prms.NextPos = curMax.Pos;
            prms.diff = curMax.diff;
            prms.vect = curMax.vect;

            stream.Pos = curMax.Pos;
            var diff = CompDiff(curr, stream.GetCurMat(), reporter.ReturnDebugVector);
            var nextVect = stream.Vectors[curMax.Pos];
            reporter.InfoReport($"===> nextX {nextVect.Vector.X} diffX {-diff.Vector.X}");
            //diff: negative if need to turn left
            //vect: positive if need to turn left
            prms.vect = new DiffVector(nextVect.Vector.X - diff.Vector.X, nextVect.Vector.Y - diff.Vector.Y);

            prms.diffVect = diff.Vector;
            prms.nextVect = nextVect.Vector;
        }



        public static  void CamTracking(Mat curImg, VidLoc.RealTimeTrackLoc realTimeTrack, PreVidStream vidProvider, IDriver driver, BreakDiffDebugReporter debugReporter)
        {
            //realTimeTrack.CurPos = image1Ind;
            realTimeTrack.LookAfter = 5;
            int origImageInd = realTimeTrack.CurPos;
            VidLoc.FindObjectDown(vidProvider, curImg, realTimeTrack, debugReporter);
            
            var lookBackCount = 0;
            while (realTimeTrack.diff < 0.5 && lookBackCount < 3)
            {
                driver.Stop();
                realTimeTrack.LongLook();
                VidLoc.FindObjectDown(vidProvider, curImg, realTimeTrack, debugReporter);
                //info.Text = text = $"Tracked vid at ${image1Ind} cam at ${image2Ind} next point ${realTimeTrack.NextPos} ${realTimeTrack.vect}  ===> diff {realTimeTrack.diff} LB {lookBackCount}";
                //Console.WriteLine(text);
                lookBackCount++;
            }

            vidProvider.Pos = origImageInd;
            driver.Track(realTimeTrack);
            if (debugReporter.DebugMode)
            {                
                Mat m1 = vidProvider.GetCurMat();
                breakAndDiff(m1, curImg, debugReporter);                
            }
        }        

        public static void breakAndDiff(Mat m1, Mat m2, BreakDiffDebugReporter reporter)
        {
            var curProcessor = new ShiftVecProcessor(m1, m2);
            //Mat res = ShiftVecDector.BreakAndNearMatches(m1, m2);
            var allDiffs = curProcessor.GetAllDiffVect();
            var vect = ShiftVecProcessor.calculateTotalVect(allDiffs);
            var average = allDiffs.Average(x => x.Diff);                        


            Mat res = curProcessor.ShowAllStepChange(allDiffs);
            reporter.Report(res, allDiffs, vect, average);
            
        }
    }

    public interface BreakDiffDebugReporter
    {
        bool DebugMode { get; }
        bool ReturnDebugVector { get; }
        void Report(Mat res, List<DiffVect> diffs, DiffVector vect, double average);
        void InfoReport(string info);
    }
}
