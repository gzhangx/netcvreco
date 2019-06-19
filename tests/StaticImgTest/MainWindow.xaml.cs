using com.veda.LinearAlg;
using Emgu.CV;
using Emgu.CV.Structure;
using netCvLib.calib3d;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using static netCvLib.calib3d.Depth;

namespace StImgTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        

        string[] images = new string[] { "7","9","17","19","47","49","57","59" };
        const string imageDir = @"C:\test\netCvReco\data\images";
        GMatrix F;
        public MainWindow()
        {
            InitializeComponent();

            var al = new List<PointFloat>();
            var ar = new List<PointFloat>();
            foreach (var iii in images)
            {
                var left = CvInvoke.Imread($"{imageDir}\\Left_{iii}.jpg");
                var right = CvInvoke.Imread($"{imageDir}\\Right_{iii}.jpg");
                var corl = convertToPF(netCvLib.calib3d.Calib.findConers(left.ToImage<Gray, Byte>()));
                al.AddRange(corl);
                var corr = convertToPF(netCvLib.calib3d.Calib.findConers(right.ToImage<Gray, Byte>()));
                ar.AddRange(corr);
                File.WriteAllLines($"{imageDir}\\Left_{iii}.txt", cornerToString(corl));
                File.WriteAllLines($"{imageDir}\\Right_{iii}.txt", cornerToString(corr));
                var ff = com.veda.LinearAlg.Calib.CalcFundm((corl), (corr));
                Console.WriteLine(ff);

            }
            Console.WriteLine("F");
            F = com.veda.LinearAlg.Calib.CalcFundm(al.ToArray(), ar.ToArray());
            Console.WriteLine(F);
        }

        static PointFloat[] convertToPF(PointF[] p)
        {
            var res = new PointFloat[p.Length];
            for(int i = 0; i < p.Length; i++)
            {
                res[i] = new PointFloat(p[i].X, p[i].Y);
            }
            return res;
        }

        static string[] cornerToString(PointFloat[] corner)
        {

            var res = new string[corner.Length];
            for (var j = 0; j < corner.Length; j++)
            {
                var sb = new StringBuilder();
                sb.Append(corner[j].X.ToString("0.000")).Append(",").Append(corner[j].Y.ToString("0.000"));
                res[j] = sb.ToString();
            }


            return res;
        }
        private void UIInvoke(Action act)
        {
            Dispatcher.BeginInvoke(act);
        }

        bool save = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            save = true;
        }

        bool show = false;
        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            show = true;
        }

        private void imgSel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var who = images[(int)imgSel.Value];
            var left = CvInvoke.Imread($"{imageDir}\\Left_{who}.jpg");
            var right = CvInvoke.Imread($"{imageDir}\\Right_{who}.jpg");

            var leftPts = convertToPF(netCvLib.calib3d.Calib.findConers(left.ToImage<Gray, Byte>()));
            foreach (var pts in leftPts)
            {
                CvInvoke.Rectangle(left, new System.Drawing.Rectangle((int)pts.X, (int)pts.Y, 2,2), new MCvScalar(0,0,255), 2);
            }

            imgLeft.Source = DisplayLib.Util.Convert(left.Bitmap);
            imgRight.Source = DisplayLib.Util.Convert(right.Bitmap);
        }
    }
    
}
