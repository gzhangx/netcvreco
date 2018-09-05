using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace netCvLib
{
    public class RoadDetector
    {
        public class Parms
        {
            public Func<Mat, Mat> filter = m=>m;
            public double threadshold1 = 50;
            public double threadshold2 = 150;
        }
        static Parms defaultParam = new Parms();
        public static Func<Mat, Mat> CreateFilter(int lowCol = 200, int highCol = 255, Action<Mat> onFilter= null)
        {
            return gray =>
            {
                var low = gray.Clone();
                low.SetTo(new Gray(lowCol).MCvScalar);
                var upper = gray.Clone();
                upper.SetTo(new Gray(highCol).MCvScalar);
                Mat filtered = new Mat();
                CvInvoke.InRange(gray, low, upper, filtered);
                if (onFilter != null) onFilter(filtered);
                return filtered;
            };
        }

        public static byte[] GetMatData(Mat mat)
        {
            byte[] res = new byte[mat.Cols * mat.Rows * mat.NumberOfChannels]; //mat.elementSize
            Marshal.Copy(mat.DataPointer, res, 0, res.Length);
            return res;
        }

        public static void SetMatData(Mat mat, byte[] data)
        {
            Marshal.Copy(data, 0, mat.DataPointer, data.Length);
        }

        public static Func<Mat, Mat> CreateFilter(Bgr lowCol, Bgr highCol, Action<Mat> onFilter = null)
        {
            return gray =>
            {
                var low = gray.Clone();
                low.SetTo((lowCol).MCvScalar);
                var upper = gray.Clone();
                upper.SetTo((highCol).MCvScalar);
                Mat filtered = new Mat();
                CvInvoke.InRange(gray, low, upper, filtered);
                if (onFilter != null) onFilter(filtered);
                return filtered;
            };
        }
        public static Mat Detect(string fname, Parms filter = null)
        {
            return Detect(CvInvoke.Imread(fname), filter);
        }
        public static Mat Detect(Mat input, Parms filters = null)
        {
            Mat gray = new Mat();
            CvInvoke.CvtColor(input, gray, ColorConversion.Bgr2Gray);


            if (filters == null) filters = defaultParam;
            Mat filtered = filters.filter(gray);            

            var edges = new Mat();
            CvInvoke.Canny(filtered, edges, filters.threadshold1, filters.threadshold2);
            return edges;
        }
    }
}
