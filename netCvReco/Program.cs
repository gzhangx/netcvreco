using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Threading;
using netCvLib;

namespace netCvReco
{
    class Program
    {
        static Mat LoadGray(string name)
        {
            Mat road = CvInvoke.Imread(name);
            Mat gray = new Mat();
            CvInvoke.CvtColor(road, gray, ColorConversion.Bgr2Gray);
            return gray;
        }

        static Mat MaskImg(Mat edges, String maskFileName = "edgesmask.png")
        {
            var edgesmask = LoadGray(maskFileName);
            var maskedEdges = new Mat();
            CvInvoke.BitwiseAnd(edges, edgesmask, maskedEdges);            
            return maskedEdges;
            //CvInvoke.BitwiseOr(edges, maskedEdges, edgesmask);
        }

        static void SaveVideo(string name)
        {
            var cap = new VideoCapture(name);
            var fc = cap.GetCaptureProperty(CapProp.FrameCount);
            Console.WriteLine("frame count " + fc);
            for (var i = 0; i < fc; i++)
            {
                cap.SetCaptureProperty(CapProp.PosFrames, i);
                var capedi = cap.QueryFrame();
                capedi.Save("vid"+i+".jpg");
            }
            //cap.ImageGrabbed += Cap_ImageGrabbed;            
            cap.Dispose();
        }

        private static void Cap_ImageGrabbed(object sender, EventArgs e)
        {
            Console.WriteLine("frame grabed");
        }

        static void Main(string[] args)
        {
            int[] res = RMatFilter.GetRoadMeanTest();
            for(var i =0; i < res.Length;i++)
            {
                //if (res[i] != 0) Console.WriteLine(i + "," + res[i]);
            }
            return;
            //SaveVideo(@"D:\gang\iphone\2018-05-28\IMG_3050.MOV");
            String win1 = "Test Window"; //The name of the window
            CvInvoke.NamedWindow(win1); //Create the window using the specific name

            Mat img = new Mat(200, 400, DepthType.Cv8U, 3); //Create a 3 channel image of 400x200
            img.SetTo(new Bgr(255, 0, 0).MCvScalar); // set it to Blue color

            //Mat road = CvInvoke.Imread("road.jpeg");
            Mat road = CvInvoke.Imread("vid405.jpg");
            Mat gray = new Mat();
            CvInvoke.CvtColor(road, gray, ColorConversion.Bgr2Gray);

            var low = gray.Clone();
            low.SetTo(new Gray(200).MCvScalar);
            var upper = gray.Clone();
            upper.SetTo(new Gray(255).MCvScalar);
            Mat filtered = new Mat();
            CvInvoke.InRange(gray, low, upper, filtered);
            filtered.CopyTo(img);
            //gray.Save("gray.png");

            //Draw "Hello, world." on the image using the specific font
            CvInvoke.PutText(
               img,
               "Hello, world",
               new System.Drawing.Point(10, 80),
               FontFace.HersheyComplex,
               1.0,
               new Bgr(0, 255, 0).MCvScalar);


            var edges = new Mat();
            CvInvoke.Canny(filtered, edges, 1, 2);
            //edges = MaskImg(edges);
            //CvInvoke.Imshow(win1, RoadDetector.Detect("vid405.jpg")); //Show the image
            CvInvoke.Imshow(win1, edges); //Show the image

            var lines = new Mat();
            CvInvoke.HoughLinesP(edges, lines, 1, Math.PI/180, 10);


            img.SetTo(new Bgr(255, 0, 0).MCvScalar); // set it to Blue color
            for (var i = 0; i < lines.Rows; i++)
            {
                var v4i = lines.Row(i);
                var data = v4i.GetData();
                var ints = new int[4];
                for (var index = 0; index < ints.Length; index++)
                {
                    ints[index] = (int)data.GetValue(index); //BitConverter.ToInt32(data, index * sizeof(int));
                }

                CvInvoke.Line(img, new System.Drawing.Point(ints[0], ints[1]),
                    new System.Drawing.Point(ints[2], ints[3]), new Gray(0).MCvScalar);
            }
            
            //CvInvoke.Imshow(win1, img); //Show the image
            edges.Save("edges.png");
            CvInvoke.WaitKey(0);  //Wait for the key pressing event
            CvInvoke.DestroyWindow(win1); //Destroy the window if key is pressed
        }
    }
}
