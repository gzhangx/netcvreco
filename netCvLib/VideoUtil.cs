using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netCvLib
{
    public class VideoUtil
    {
        public const string VIDINFOFILE = "videocnt.txt";
        public static void SaveVideo(string name, Func<Mat,Mat> matAct, Action<int,int> reporter)
        {
            var cap = new VideoCapture(name);
            var fc = cap.GetCaptureProperty(CapProp.FrameCount);
            Console.WriteLine("frame count " + fc);
            for (var i = 0; i < fc; i++)
            {
                cap.SetCaptureProperty(CapProp.PosFrames, i);
                var capedi = cap.QueryFrame();
                Console.WriteLine("saving " + i+"/"+fc);
                capedi = matAct(capedi);
                capedi.Save("vid" + i + ".jpg");
                reporter(i, (int)fc);
            }
            File.WriteAllText(VIDINFOFILE, fc.ToString());
            //cap.ImageGrabbed += Cap_ImageGrabbed;            
            cap.Dispose();
        }
    }
}
