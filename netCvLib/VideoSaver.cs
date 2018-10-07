using Emgu.CV;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netCvLib
{
    public class StdVideoSaver
    {
        public const string VECT_FILE = "vect.txt";
        protected string folderName;
        protected int curVidNum = 0;
        protected Mat prevMat = null;
        protected ISaveVideoReport Reporter;
        public StdVideoSaver(string folder, ISaveVideoReport reporter)
        {
            folderName = folder;
            Directory.CreateDirectory(folder);
            File.WriteAllText(VectFileName, "");
            Reporter = reporter;
        }

        protected string VectFileName
        {
            get
            {
                return $"{folderName}\\{VECT_FILE}";
            }
        }
        public void SaveVid(Mat mat)
        {
            ShiftVecDector.ResizeToStdSize(mat);
            if (prevMat != null)
            {
                var diff = VidLoc.CompDiff(prevMat, mat, null);
                Reporter.InfoReport($"At {curVidNum} diff {diff.Vector.X} {diff.Vector.Y}");
                if (Math.Abs(diff.Vector.X) < 0.01 && Math.Abs(diff.Vector.Y) < 0.01)
                {
                    return;
                }                
                File.AppendAllText(VectFileName, $"{diff.Vector.X} {diff.Vector.Y} {diff.Vector.Diff}\n");
                Reporter.ShowProg(curVidNum, $"{diff.Vector.X} {diff.Vector.Y}");
                prevMat.Dispose();
            }            
            prevMat = new Mat();
            mat.CopyTo(prevMat);
            mat.Save($"{folderName}\\vid{curVidNum}.jpg");            
            Reporter.ShowProg(curVidNum,"");
            curVidNum++;
            File.WriteAllText($"{folderName}\\{VideoUtil.VIDINFOFILE}", curVidNum.ToString());
        }
    }

    public interface ISaveVideoReport
    {
        void ShowProg(int i, string s);
        void InfoReport(string s);
    }
}
