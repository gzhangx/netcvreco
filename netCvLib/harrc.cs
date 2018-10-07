using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Drawing;

namespace netCvLib
{
    public class GZHarC
    {
        CascadeClassifier haar;
        public GZHarC()
        {
            haar = new CascadeClassifier("haarcascade_frontalface_alt2.xml");            
        }

        public Rectangle[] Detect(Mat m)
        {
            return haar.DetectMultiScale(m);
        }
    }
}
