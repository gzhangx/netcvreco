using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StImgTest
{
    /// <summary>
    /// Interaction logic for Window3dProj.xaml
    /// </summary>
    public partial class Window3dProj : Window
    {
        Simple2dProj proj = new Simple2dProj(100);
        const int width = 512;
        const int height = 512;
        const int centerX = width / 2;
        const int centerY = height / 2;
        byte[] data = new byte[width * height];
        WriteableBitmap bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);

        protected MCvPoint3D32f[] pts = new MCvPoint3D32f[0];
        public Window3dProj()
        {
            InitializeComponent();
            LoadFake();
        }

        private void LoadFake()
        {
            var lines = File.ReadAllLines(@"C:\test\netCvReco.run\points.txt");
            List<MCvPoint3D32f> dt = new List<MCvPoint3D32f>();
            foreach (var l in lines)
            {
                var xyz = l.Split(',');
                dt.Add(new MCvPoint3D32f(float.Parse(xyz[0]), float.Parse(xyz[1]), float.Parse(xyz[2])));
            }
            pts = dt.ToArray();
            WriteData();
        }

        public void SetData(MCvPoint3D32f[] points)
        {
            pts = points;
            WriteData();
        }

        private void rotX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (rotX == null) return;
            if (lblXRot == null) return;
            lblXRot.Text = "X" + rotX.Value.ToString("0.0");
            proj.rotX = (float)(rotX.Value / 180 * Math.PI);
            WriteData();
        }

        private void sliderZ_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderZ == null) return;
            if (lblZVal == null) return;
            proj.setCamPlanZ((float)sliderZ.Value);
            lblZVal.Text = "Z" + sliderZ.Value.ToString("0.0");
            WriteData();
        }

        public void WriteData()
        {
            float min = 1000000;
            float max = 0;
            foreach (var pt in pts)
            {                
                if (pt.Z < min)
                {
                    min = pt.Z;
                }else if (pt.Z > max)
                {
                    max = pt.Z;
                }
            }
            int discarded = 0;
            for(var i = 0; i < data.Length; i++)
            {
                data[i] = 0;
            }
            
            foreach (var pt in pts)
            {
                var point = proj.proj(pt);
                var z = (pt.Z - min) * 255 / (max - min);
                int x = point.X + centerX;
                int y = point.Y + centerY;
                if (x > 0 && x < width && y > 0 && y < height)
                {
                    data[(y * width + x)] = (byte)z;
                }else
                {
                    discarded++;
                }
            }
            
            Console.WriteLine($"Discarded {discarded} min =${min} max=${max}");
            bmp.WritePixels(new Int32Rect(0,0,width,height), data, width, 0);
            img.Source = bmp;
        }
    }
}
