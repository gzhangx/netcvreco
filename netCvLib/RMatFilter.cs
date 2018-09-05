using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netCvLib
{
    public static class RMatFilter
    {
        const int MAXDIFF = 15;
        const int MAXGDIFF = 2;
        const int MAXTOP = 200;
        const byte GRAY = 150;

        public static void FilterRoadByColor(Mat mat)
        {
            var data = RoadDetector.GetMatData(mat);
            int pos = 0;            
            //const int TOODARK = 120;
            
            for (var y = 0; y < mat.Rows; y++)
            {
                for (var x = 0; x < mat.Cols; x++)
                {
                    var b = data[pos];
                    var g = data[pos + 1];
                    var r = data[pos + 2];
                    var gbdiff = g - b;
                    var grdiff = g - r;
                    var gbdiffabs = Math.Abs(gbdiff);
                    var grdiffabs = Math.Abs(grdiff);
                    if (gbdiff> MAXGDIFF && grdiff > MAXGDIFF)
                    {
                        data[pos] = data[pos + 1] = data[pos + 2] = 0;
                        data[pos + 1] = 255;
                    }else if (g > MAXTOP && b > MAXTOP && r > MAXTOP)
                    {
                        data[pos] = data[pos + 1] = data[pos + 2] = 255;
                    }else
                    if (gbdiffabs < MAXDIFF && grdiffabs < MAXDIFF)
                    {
                        data[pos] = data[pos + 1] = data[pos + 2] = GRAY;
                    }
                    pos += mat.ElementSize;
                }
            }
            RoadDetector.SetMatData(mat, data);
        }

        //Not good
        public static void ReduceColor(Mat mat, int div = 64)
        {
            var data = RoadDetector.GetMatData(mat);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(data[i] / div * div);
            }
            RoadDetector.SetMatData(mat, data);
        }

        
        public class RoadStruc
        {
            public System.Drawing.Point leftStart, leftEnd, rightStart, rightEnd;
        }

        public static RoadStruc FilterRoadByMean(Mat mat)
        {
            var data = RoadDetector.GetMatData(mat);

            const int SPACING = 100;
            int xspaceEnd = mat.Cols - SPACING;
            int ystart = 400;
            int yspacingNoe2 = 2;
            bool[,] spacing = new bool[yspacingNoe2, (xspaceEnd/SPACING)+1];
            int aty = 0;
            for (var y = ystart; y < ystart + (yspacingNoe2*SPACING); y += SPACING, aty++)
            {                
                for (var x = 0; x < xspaceEnd; x += SPACING)
                {
                    spacing[aty, x/SPACING] = GetRoadMean(data, mat, x, y, SPACING, SPACING);
                }
            }


            int middle = xspaceEnd / SPACING / 2;
            int[] leftX = new int[2];
            int[] rightX = new int[2];
            for (int y = 0; y < yspacingNoe2; y++)
            {
                if (spacing[y,middle])
                {
                    int left = middle;
                    while (left>=1 && spacing[y,left - 1]) left--;
                    leftX[y] = left;
                    int right = middle;
                    while (right <= spacing.GetUpperBound(1)-1 && spacing[y, right+1]) right++;
                    rightX[y] = right;
                }
            }
            RoadDetector.SetMatData(mat, data);
            Func<int, int> tran = x => x + (SPACING / 2);
            return new RoadStruc
            {
                leftStart = new System.Drawing.Point(tran(leftX[0]*100), tran(ystart)),
                leftEnd = new System.Drawing.Point(tran(leftX[1] * 100), tran(ystart+SPACING)),

                rightStart = new System.Drawing.Point(tran(rightX[0] * 100), tran(ystart)),
                rightEnd = new System.Drawing.Point(tran(rightX[1] * 100), tran(ystart + SPACING)),
            };
        }
        public static bool GetRoadMean(byte[] data, Mat mean, int startx, int starty, int w, int h)
        {            
            int ROWLEN = mean.Cols * mean.ElementSize;
            int[] colorCount = new int[256];
            int START = (starty * ROWLEN) + (startx * mean.ElementSize);
            if (starty + h > mean.Rows)
            {
                h = mean.Rows - starty;
            }
            if (h <= 0) throw new ArgumentException($"Bad starty and h {starty} {h}");

            if (startx + w > mean.Cols)
            {
                w = mean.Cols - startx;
            }
            if (w <= 0) throw new ArgumentException($"Bad startx and w {startx} {w}");
            int yStartPos = START;
            int goodCount = 0;
            for (int y = 0; y < h; y++)
            {
                int i = yStartPos;
                for (int x = 0; x < w; x++)
                {
                    var b = data[i];
                    var g = data[i + 1];
                    var r = data[i + 2];

                    colorCount[(b + g + r) / 3]++;                    


                    var gbdiff = g - b;
                    var grdiff = g - r;
                    var gbdiffabs = Math.Abs(gbdiff);
                    var grdiffabs = Math.Abs(grdiff);
                    if (gbdiff > MAXGDIFF && grdiff > MAXGDIFF)
                    {
                        data[i] = data[i + 1] = data[i + 2] = 0;
                        data[i + 1] = 255;
                    }
                    else if (g > MAXTOP && b > MAXTOP && r > MAXTOP)
                    {
                        data[i] = data[i + 1] = data[i + 2] = 255;
                    }
                    else if (gbdiffabs < MAXDIFF && grdiffabs < MAXDIFF)
                    {
                        data[i] = data[i + 1] = data[i + 2] = GRAY;
                        goodCount++;
                    }

                    i += mean.ElementSize;
                }
                yStartPos += ROWLEN;
            }


            int peakPos = 0;
            int peakVal = 0;
            long total = 0;
            for (var i = 0; i < colorCount.Length; i++)
            {
                var cur = colorCount[i];
                total += cur;
                if (cur > peakVal)
                {
                    peakVal = cur;
                    peakPos = i;
                }
            }
            int peakSpread = colorCount[peakPos];
           // Console.WriteLine("peak spread at " + peakPos + " is " + peakSpread + " " + (peakSpread * 1.0 / total));
            for (var i = 1; i < 20; i++)
            {
                if (peakPos + i < colorCount.Length)
                    peakSpread += colorCount[peakPos + i];
                if (peakPos - i >= 0)
                    peakSpread += colorCount[peakPos - i];
                //Console.WriteLine("peak spread at " + i + " is " + peakSpread + " " + (peakSpread * 1.0 / total));
            }

            Console.WriteLine($"peak spread ${starty} ${startx} peakSpread={ (peakSpread * 1.0 / total)} goodCount={(goodCount * 1.0 / total)}");
            //peakSpread*1.0/total > 0.7
            if ( (goodCount*1.0/total>0.40))
            {
                yStartPos = START;
                for (int y = 0; y < h; y++)
                {
                    int i = yStartPos;
                    for (int x = 0; x < w; x++)
                    {
                        data[i] = data[i + 1] = data[i + 2] = GRAY;
                        data[i] = data[i + 1] = 0;
                        data[i + 2] = GRAY;
                        i += mean.ElementSize;
                    }
                    yStartPos += ROWLEN;
                }
                return true;
            }
            return false;
        }
        public static int[] GetRoadMeanTest()
        {
            Mat mean = CvInvoke.Imread("roadmean.png");
            var data = RoadDetector.GetMatData(mean);
            int maxAdjBdiff = 0;
            int maxAdjGdiff = 0;
            int maxAdjRdiff = 0;
           

            int maxBGdiff = 0;
            int maxGRdiff = 0;
            int maxBRdiff = 0;

            int ROWLEN = mean.Rows * mean.ElementSize;
            float totalB = 0;
            float totalG = 0;
            float totalR = 0;
            int maxB = 0;
            int maxG = 0;
            int maxR = 0;
            int[] colorCount = new int[255];
            for (int i = 3; i < data.Length - ROWLEN; i+= mean.ElementSize)
            {
                var b = data[i];
                var g = data[i + 1];
                var r = data[i + 2];

                var pi = i - mean.ElementSize;
                var pb = data[pi];
                var pg = data[pi + 1];
                var pr = data[pi + 2];

                var bi = i + ROWLEN;

                totalB += b;
                totalG += g;
                totalR += r;
                if (b > maxB) maxB = b;
                if (g > maxG) maxG = g;
                if (r > maxR) maxR = r;

                var bgdiff = Math.Abs(b - g);
                var grdiff = Math.Abs(r - g);
                var brdiff = Math.Abs(b - r);
                if (bgdiff > maxBGdiff) maxBGdiff = bgdiff;
                if (grdiff > maxGRdiff) maxGRdiff = grdiff;
                if (brdiff > maxBRdiff) maxBRdiff = brdiff;

                var bpdiff = Math.Abs(b - pb);
                if (bpdiff > maxAdjBdiff) maxAdjBdiff = bpdiff;

                var gpdiff = Math.Abs(g - pg);
                if (gpdiff > maxAdjGdiff) maxAdjGdiff = gpdiff;
                var rpdiff = Math.Abs(r - pr);
                if (rpdiff > maxAdjRdiff) maxAdjRdiff = rpdiff;
                colorCount[(b + g + r) / 3]++;
            }


            int peakPos = 0;
            int peakVal = 0;
            long total = 0;
            for (var i = 0; i < colorCount.Length; i++)
            {
                var cur = colorCount[i];
                total += cur;
                if (cur > peakVal)
                {
                    peakVal = cur;
                    peakPos = i;
                }
            }
            int peakSpread = colorCount[peakPos];
            Console.WriteLine("peak spread at " + peakPos + " is " + peakSpread + " " + (peakSpread * 1.0 / total));
            for (var i = 1; i < 20;i++)
            {
                if (peakPos + i < colorCount.Length)
                    peakSpread += colorCount[peakPos + i];
                if (peakPos - i >= 0)
                    peakSpread += colorCount[peakPos - i];
                Console.WriteLine("peak spread at " + i + " is " + peakSpread + " " + (peakSpread*1.0 / total));
            }
            return colorCount;
        }
    }
}
