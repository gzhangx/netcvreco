using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netCvLib
{
    public class DiffVect
    {
        public int VidPos { get; set; } //not related, debug video pos
        public Point Location { get; set; }
        public Rectangle SrcRect { get; set; }

        public DiffVector Vector { get; set; }
        public override string ToString()
        {
            if (Vector == null) Vector = new DiffVector(0,0,0);
            return $"{Vector.X},{Vector.Y}  {Vector.Diff.ToString("0.00")}";
        }
    }

    public class DiffDebug
    {
        public DiffVect Vect { get; set; }
        public Mat orig { get; set; }
        public Mat area { get; set; }
        public Image<Gray, float> diffMap { get; set; }
        public Rectangle SrcRect { get; set; }
        public Rectangle CompareToRect { get; set; }
    }
    public class DiffVector
    {
        public double X { get; protected set; }
        public double Y { get; protected set; }
        public double Diff { get; set; }
        public DiffVector(double x, double y, double diff)
        {
            X = x;
            Y = y;
            Diff = diff;
        }
        public override string ToString()
        {            
            return $"({X},{Y})";
        }
    }
    public static class ShiftVecDector
    {
        public const int STDWIDTH = 128;
        public static Mat ToGray(this Mat input)
        {
            if (input.ElementSize == 1) return input;
            Mat gray = new Mat();
            CvInvoke.CvtColor(input, gray, ColorConversion.Bgr2Gray);
            return gray;
        }

        public static void ResizeToStdSize(this Mat input)
        {
            if (input == null) return;
            if (input.Width > STDWIDTH)
            {
                double scale = ((double)STDWIDTH) / input.Width;
                int newH = (int)(input.Height * scale);
                if (newH <= 0) newH = 1;
                CvInvoke.Resize(input, input, new Size(STDWIDTH, newH));
             }
        }

        private static int CalcBound(int v, int size,  int ext, int max)
        {
            int w = ext + size + ext;
            int maxW = max - v;
            if (maxW < 0) return maxW;            
            if (w > maxW) w = maxW;
            return w;
        }
        public static Rectangle getExtCropRect(Mat compareTo, int x, int y, int cutSize, int cmpExtend)
        {
            int newX = x - cmpExtend;
            if (newX < 0) newX = 0;
            int newY = y - cmpExtend;
            if (newY < 0) newY = 0;
            int perfered = cutSize + cmpExtend;
            int width = CalcBound(x, cutSize, cmpExtend, compareTo.Width);
            int height = CalcBound(y, cutSize, cmpExtend, compareTo.Height); ;
            return new Rectangle(newX, newY, width, height);
        }
        //public static Mat BreakAndNearMatches(Mat input, Mat compareTo, int cutSize = STDWIDTH/10, int cmpExtend = STDWIDTH / 10)
        //{

        //    input = input.ToGray();
        //    compareTo = compareTo.ToGray();
        //    ResizeToStdSize(input);
        //    ResizeToStdSize(compareTo);
        //    //input.Save(@"d:\temp\test\resized.jpg");
        //    //List<List<Mat>> cuts = new List<List<Mat>>();
        //    List<DiffVect> difVector = new List<DiffVect>();

        //    var compareToImage = compareTo.Clone();
        //    for (int y = 0; y < input.Height - cutSize; y+= cutSize)
        //    {
        //        for (int x = 0; x < input.Width - cutSize; x+= cutSize)
        //        {
        //            var corped = new Mat(input, new Rectangle(x, y, cutSize, cutSize));
        //            var cmpToRect = getExtCropRect(compareTo, x, y, cutSize, cmpExtend);
        //            var cmpToCorp = new Mat(compareTo, cmpToRect);
        //            //corped.Save(@"d:\temp\test\" + x + "_" + y + ".jpg");
        //            var matched = cmpToCorp.ToImage<Gray, Byte>().MatchTemplate(corped.ToImage<Gray, Byte>(), TemplateMatchingType.CcoeffNormed);
        //            double[] minValues, maxValues;
        //            Point[] minLocs, maxLocs;
        //            matched.MinMax(out minValues, out maxValues, out minLocs, out maxLocs);
        //            Point maxLoc = maxLocs[0];
        //            double maxVal = maxValues[0];                    
        //            var diffVect = new DiffVect { Location = new Point(x, y), Vector = new Point(maxLoc.X - (x - cmpToRect.X), maxLoc.Y - (y - cmpToRect.Y)), Diff = maxVal };
        //            Console.WriteLine(" got  " + diffVect);
        //            difVector.Add(diffVect);
        //            corped.CopyTo(new Mat
        //                (compareToImage, new Rectangle(x + diffVect.Vector.X, y + diffVect.Vector.Y, cutSize, cutSize)));
        //        }
        //    }

        //    compareToImage.Save(@"d:\temp\test\recon.jpg");
        //    return compareToImage;
        //}
    }

    public class ShiftVecProcessor
    {
        protected Mat inputOrig;
        protected Mat compareToOrig;
        public Mat input { get; protected set; }
        public Mat compareTo { get; protected set; }

        public DiffVect DiffVectorCalculated { get; protected set; } //only available after calling GetAllDiffVect
        public NumberGrouper.NumberRange NumberRangeX {get; protected set; }//only available after calling GetAllDiffVect
        public int CutSize
        {
            get
            {
                return ShiftVecDector.STDWIDTH / 8;
            }
        }
        public int CmpExtend
        {
            get
            {
                return ShiftVecDector.STDWIDTH / 10;
            }
        }
        public ShiftVecProcessor(Mat inputOrig, Mat compareToOrig)
        {
            this.inputOrig = inputOrig;
            this.compareToOrig = compareToOrig;
            input = inputOrig.ToGray();
            compareTo = compareToOrig.ToGray();
            input.ResizeToStdSize();
            compareTo.ResizeToStdSize();
        }

        public DiffVect CalculateDiffVect(int x, int y)
        {
            return CalculateDiffVectDbg(new Rectangle(x, y, CutSize, CutSize), null);
        }
        public DiffVect CalculateDiffVect()
        {
            var cut2 = CutSize * 2;
            return CalculateDiffVectDbg(new Rectangle(CutSize, CutSize, compareTo.Width - cut2, compareTo.Height - cut2), null);
        }
        public DiffVect CalculateDiffVectDbg(Rectangle srcRect, Action<DiffDebug> dbgAct)
        {
            using (var corped = new Mat(input, srcRect))
            {
                //var cmpToRect = ShiftVecDector.getExtCropRect(compareTo, x, y, CutSize, CmpExtend);
                //using (var cmpToCorp = new Mat(compareTo, cmpToRect))
                var cmpToCorp = compareTo;
                {
                    //corped.Save(@"d:\temp\test\" + x + "_" + y + ".jpg");
                    using (var matched = cmpToCorp.ToImage<Gray, Byte>().MatchTemplate(corped.ToImage<Gray, Byte>(), TemplateMatchingType.CcoeffNormed))
                    {
                        double[] minValues, maxValues;
                        Point[] minLocs, maxLocs;
                        matched.MinMax(out minValues, out maxValues, out minLocs, out maxLocs);
                        Point maxLoc = maxLocs[0];
                        double maxVal = maxValues[0];
                        var diffVect = new DiffVect { Location = srcRect.Location, SrcRect = srcRect, Vector = new DiffVector(maxLoc.X - srcRect.X, maxLoc.Y - srcRect.Y, maxVal)};
                        //Console.WriteLine(" got  " + diffVect);
                        if (dbgAct != null) dbgAct(new DiffDebug
                        {
                            Vect = diffVect,
                            orig = corped,
                            area = cmpToCorp,
                            diffMap = matched,
                            SrcRect = srcRect,
                            //CompareToRect = cmpToRect,
                        });
                        return diffVect;
                    }
                }
            }
        }
        /*
        public List<DiffVect> GetAllDiffVectOldMultiSmall()
        {
            DiffVectorCalculated = new List<DiffVect>();
            int boundReduce = CutSize + CmpExtend;
            for (int y = CutSize; y < input.Height - boundReduce; y += CutSize)
            {
                for (int x = CutSize; x < input.Width - boundReduce; x += CutSize)
                {
                    var diffVect = CalculateDiffVect(x, y);
                    //Console.WriteLine(" got  " + diffVect);
                    DiffVectorCalculated.Add(diffVect);
                    //corped.CopyTo(new Mat(compareToImage, new Rectangle(x + diffVect.Vector.X, y + diffVect.Vector.Y, cutSize, cutSize)));
                }
            }

            int[] xvalues = new int[DiffVectorCalculated.Count];
            for (var i = 0; i < DiffVectorCalculated.Count;i++)
            {
                xvalues[i] = DiffVectorCalculated[i].Vector.X;
            }
            NumberGrouper gp = new NumberGrouper(CutSize);
            var goodXV = gp.Process(xvalues);
            NumberRangeX = goodXV[0];
            return DiffVectorCalculated;
        }
        */

        public DiffVect GetAllDiffVect()
        {
            //DiffVectorCalculated = new List<DiffVect>();                      
            DiffVectorCalculated = CalculateDiffVect();
            //Console.WriteLine(" got  " + diffVect);
            //DiffVectorCalculated.Add(diffVect);
                                
            return DiffVectorCalculated;
        }

        public static Point ClonePointWithYOff(Point p, int y)
        {
            return new Point(p.X, p.Y + y);
        }
        public Mat ShowStepChange(DiffVect diffVect, Mat compareToImage)
        {
            var x = diffVect.Location.X;
            int y = diffVect.Location.Y;
            using (var corped = new Mat(input, diffVect.SrcRect))
            {
                if (compareToImage == null)
                    compareToImage = compareTo.Clone();
                //CvInvoke.Rectangle(compareToImage, new Rectangle(x, y, CutSize, CutSize), new MCvScalar(0));

                var toRect = new Rectangle(x + (int)diffVect.Vector.X, y + (int)diffVect.Vector.Y, diffVect.SrcRect.Width, diffVect.SrcRect.Height);
                corped.CopyTo(new Mat(compareToImage, toRect));
                //CvInvoke.Rectangle(compareToImage, new Rectangle(x, y, CutSize, CutSize), new MCvScalar(200));
                CvInvoke.Rectangle(compareToImage, toRect, new MCvScalar(100));
                CvInvoke.Line(compareToImage, new Point(x, y), toRect.Location, new MCvScalar(150));
                CvInvoke.PutText(compareToImage, diffVect.Vector.Diff.ToString("0.00"), ClonePointWithYOff(toRect.Location, 10), FontFace.HersheyPlain, 1, new MCvScalar(10));
                CvInvoke.PutText(compareToImage, diffVect.Vector.X.ToString("0.00"), ClonePointWithYOff(toRect.Location, 20), FontFace.HersheyPlain, 1, new MCvScalar(10));
                return compareToImage;
            }
        }

        public Mat ShowAllStepChange(DiffVect diffs)
        {
            var compareToImage = compareTo.Clone();
            {
                ShowStepChange(diffs, compareToImage);
            }
            return compareToImage;
        }

        //public static DiffVector calculateTotalVect(DiffVect d)
        //{
        //    var dx = d.Vector.X; // ((double)allDiffs.Sum(d => d.Vector.X)) / allDiffs.Count;
        //    var duy = ((double)allDiffs.Take(allDiffs.Count / 2).Sum(d => d.Vector.Y)) / allDiffs.Count * 2;
        //    var dly = ((double)allDiffs.Skip(allDiffs.Count / 2).Sum(d => d.Vector.Y)) / allDiffs.Count * 2;
        //    return new DiffVector(dx, duy - dly);
        //    return new DiffVector(dx, d.Vector.Y);
        //}

        //public DiffVector calculateTotalVect()
        //{
        //    return calculateTotalVect(DiffVectorCalculated);
        //}
    }
}
