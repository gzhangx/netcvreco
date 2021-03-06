﻿using netCvLib;
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
        public const string VECT_FILE = StdVideoSaver.VECT_FILE;
        protected string filePath;
        public VideoProvider(string path)
        {
            filePath = path;
            LoadDiffVectors();
        }
        protected List<DiffVector> vectors = new List<DiffVector>();
        protected void LoadDiffVectors()
        {
            try
            {
                var lines = File.ReadAllLines($"{filePath}\\{VECT_FILE}");
                foreach(var line in lines)
                {
                    var pars = line.Split(' ');
                    vectors.Add(new DiffVector(double.Parse(pars[0]), double.Parse(pars[1]), double.Parse(pars[2])));
                }
            } catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
        }

        public List<DiffVector> Vectors
        {
            get
            {
                return vectors;
            }
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
                    try
                    {
                        total = Convert.ToInt32(File.ReadAllText($"{filePath}\\{VideoUtil.VIDINFOFILE}"));
                    }
                    catch { }
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
