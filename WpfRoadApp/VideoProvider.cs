using netCvLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using System.IO;

namespace WpfRoadApp
{
    public class VideoProvider : PreVidStream
    {
        protected string filePath;
        public VideoProvider(string path)
        {
            filePath = path;
        }
        protected int pos = 0;
        protected int total = -1;  
        public int Pos
        {
            get
            {
                return pos;
            }

            set
            {
                pos = value;
                if (pos < 0) pos = 0;
                if (pos > Total - 1) pos = Total - 1;
            }
        }

        public int Total
        {
            get
            {
                if (total < 0)
                {
                    total = Convert.ToInt32(File.ReadAllText($"{filePath}\\{VideoUtil.VIDINFOFILE}"));
                }
                return total;
            }
        }

        public string GetPath(int i)
        {
            if (i < 0) i = 0;
            i = i % 984;
            var path = System.IO.Directory.GetCurrentDirectory() +  $"\\{filePath}\\vid{i}.jpg";
            return path;
        }
        public Mat GetCurMat()
        {
            return CvInvoke.Imread(GetPath(Pos));
        }
    }
}
