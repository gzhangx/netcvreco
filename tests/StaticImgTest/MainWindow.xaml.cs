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
            var rightPts = convertToPF(netCvLib.calib3d.Calib.findConers(right.ToImage<Gray, Byte>()));
            var rnd = new Random();
            Func<int> nextClr = () => (int)(rnd.NextDouble() * 255);
            for (var i = 0; i < leftPts.Length; i++)
            {
                var pts = leftPts[i];
                var rpts = rightPts[i];
                var clr = new MCvScalar(nextClr(), nextClr(), nextClr());
                CvInvoke.Rectangle(left, new System.Drawing.Rectangle((int)pts.X, (int)pts.Y, 2,2), clr, 2);
                var gm = F.dot(new GMatrix(new double[3, 1] { { pts.X }, { pts.Y }, { 1  } }));
                DrawEpl(right, gm, clr, rpts);                
            }

            imgLeft.Source = DisplayLib.Util.Convert(left.Bitmap);
            imgRight.Source = DisplayLib.Util.Convert(right.Bitmap);
        }

        private void DrawEpl(Mat img, GMatrix m, MCvScalar clr, PointFloat rpts)
        {
            var ms = m.storage;
            var a = ms[0][0];
            var b = ms[1][0];
            var c = ms[2][0];
            Console.WriteLine(m);
            if (Math.Abs(a) > Math.Abs(b))
            {
                Func<System.Drawing.Point, System.Drawing.Point> fixv = vv =>
                 {
                     //vv.X = Math.Abs(vv.X);
                     return vv;
                 };
                Func<int, System.Drawing.Point> toX = y=> new System.Drawing.Point((int)((-c - (b * y)) / a), y);
                var p1 = fixv(toX(0));
                var p2 = fixv(toX(img.Height));
                var sum = (rpts.X * a) + (rpts.Y * b) + c;
                Console.WriteLine($"Drawinga {p1.X},{p1.Y}      {p2.X},{p2.Y}     {sum}");
                CvInvoke.Line(img, p1, p2, clr, 2);                
            }else
            {
                Func<System.Drawing.Point, System.Drawing.Point> fixv = vv =>
                {
                    //vv.Y = Math.Abs(vv.Y);
                    return vv;
                };
                Func<int, System.Drawing.Point> toY = x => new System.Drawing.Point(x,(int)((-c - (a * x)) / b));
                var p1 = fixv(toY(0));
                var p2 = fixv(toY(img.Width));
                var sum = (rpts.X * a) + (rpts.Y * b) + c;
                CvInvoke.Line(img, p1, p2, clr, 2);
                Console.WriteLine($"Drawingb {p1.X},{p1.Y}      {p2.X},{p2.Y}         {sum}");
            }
        }
    }
    
}
