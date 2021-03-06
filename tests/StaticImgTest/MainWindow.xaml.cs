﻿using com.veda.LinearAlg;
using Emgu.CV;
using Emgu.CV.Structure;
using netCvLib.calib3d;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using static com.veda.LinearAlg.CalibRect;
using static netCvLib.calib3d.Depth;

namespace StImgTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        

        string[] images = new string[] { "0","1","2","3","4","5","6","7" };
        const string imageDir = @"C:\test\netCvReco\data\images";
        RectifyResult calres;
        GMatrix F;
        CheckBox[] cbs = new CheckBox[17];
        bool[] onChecks = new bool[17];
        int who = 0;
        public MainWindow()
        {
            InitializeComponent();

            var al = new List<PointFloat>();
            var ar = new List<PointFloat>();
            List<StereoPoints> allPts = new List<StereoPoints>();
            PointFloat imgSize = null;
            foreach (var iii in images)
            {
                var left = CvInvoke.Imread($"{imageDir}\\Left_{iii}.jpg");
                imgSize = new PointFloat(left.Width, left.Height);
                var right = CvInvoke.Imread($"{imageDir}\\Right_{iii}.jpg");
                var corl = convertToPF(netCvLib.calib3d.Calib.findConers(left.ToImage<Gray, Byte>()));
                al.AddRange(corl);                
                var corr = convertToPF(netCvLib.calib3d.Calib.findConers(right.ToImage<Gray, Byte>()));
                ar.AddRange(corr);

                allPts.Add(new StereoPoints { Left = corl, Right = corr });
                File.WriteAllLines($"{imageDir}\\Left_{iii}.txt", cornerToString(corl));
                File.WriteAllLines($"{imageDir}\\Right_{iii}.txt", cornerToString(corr));
                var ff = com.veda.LinearAlg.Calib.CalcFundm((corl), (corr));
                //Console.WriteLine(ff);

            }
            Console.WriteLine("F");
            F = com.veda.LinearAlg.Calib.CalcFundm(al.ToArray(), ar.ToArray());
            Console.WriteLine(F);


            calres = CalibRect.Rectify(allPts, imgSize);
            Console.WriteLine("Callres");
            Console.WriteLine(calres.F);
            Console.WriteLine(calres.el.X.ToString("0.00") + " " + calres.el.Y.ToString("0.00"));
            Console.WriteLine(calres.LeftIntrinics);
            Console.WriteLine(calres.RightIntrinics);
            Console.WriteLine(calres.H1);
            Console.WriteLine(calres.H2);

            Console.WriteLine("E");
            Console.WriteLine(calres.E);
            var rtl = calres.GetRT(calres.E);
            Console.WriteLine("RT from left");
            Console.WriteLine(rtl.R);
            Console.WriteLine(rtl.T);

            who = 0;
            imgSelFunc();

            for (int i = 0; i < cbs.Length; i++)
            {
                var cb = new CheckBox();
                cb.Name = "chkEpl_" + i;                
                cb.IsChecked = false;
                cb.Checked += Cb_Checked;
                cb.Unchecked += Cb_Checked;               
                cbs[i] = cb;
                onChecks[i] = false;
            }
            foreach (var cb in cbs)
                stkEpoles.Children.Add(cb);
        }

        private void Cb_Checked(object sender, RoutedEventArgs e)
        {
            var cb = (CheckBox)sender;
            var nu = cb.Name.Substring(7);
            onChecks[Convert.ToInt32(nu)] = cb.IsChecked.Value;

            imgSelFunc();
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
            who = (int)imgSel.Value;
            imgSelFunc();
        }

        MCvScalar[] colors;
        private void imgSelFunc()
        {
            var who = images[this.who];
            //var who = images[(int)imgSel.Value];
            var left = CvInvoke.Imread($"{imageDir}\\Left_{who}.jpg");
            var right = CvInvoke.Imread($"{imageDir}\\Right_{who}.jpg");

            var leftPts = convertToPF(netCvLib.calib3d.Calib.findConers(left.ToImage<Gray, Byte>()));
            var rightPts = convertToPF(netCvLib.calib3d.Calib.findConers(right.ToImage<Gray, Byte>()));
            var rnd = new Random();
            Func<int> nextClr = () => (int)(rnd.NextDouble() * 255);
            if (colors == null)
            {
                colors = new MCvScalar[leftPts.Length];
                for (var i = 0; i < leftPts.Length; i++)
                {
                    var clr = new MCvScalar(nextClr(), nextClr(), nextClr());
                    colors[i] = clr;
                }
            }
            DrawEpl(leftPts, PointSide.Left, rightPts, left, right);
            DrawEpl(rightPts, PointSide.Right, leftPts, right, left);


            
            imgLeftOrig.Source = DisplayLib.Util.Convert(left.Bitmap);
            imgRightOrig.Source = DisplayLib.Util.Convert(right.Bitmap);


            var epol = CalibRect.FindEpipole(leftPts, PointSide.Left, F);
            var h2 = CalibRect.GetH2(epol, new PointFloat(left.Width, left.Height));
            var h1 = CalibRect.GetH1(epol, new PointFloat(left.Width, left.Height), F, h2, leftPts, rightPts);

            h1 = calres.H1;

            //imgLeft.Source = DisplayLib.Util.Convert(TransformBmp(left.Bitmap, h1));            
            //imgRight.Source = DisplayLib.Util.Convert(TransformBmp(right.Bitmap,h2));
            imgLeft.Source = DisplayLib.Util.Convert(getTransformedImg(h1, left).Bitmap);
            imgRight.Source = DisplayLib.Util.Convert(getTransformedImg(h2, right).Bitmap);

            Console.WriteLine("h1");
            Console.WriteLine(h1);
            Console.WriteLine("h2");
            Console.WriteLine(h2);
            Console.WriteLine($"epoX={epol.X.ToString("0.00")} {epol.Y.ToString("0.00")} ");
            //imgRight.Source = DisplayLib.Util.Convert(right.Bitmap);            
        }

        static Mat getTransformedImg(GMatrix m, Mat mat)
        {
            return netCvLib.calib3d.Calib.TransformImg(m.To2DArray(), mat);
            //var size = mat.Size;
            //Mat tm = new Mat();
            //Matrix<double> tranMat = new Matrix<double>(m.To2DArray());
            //CvInvoke.WarpPerspective(mat, tm, tranMat, size);
            //return tm;
        }

        public Bitmap TransformBmp(Bitmap bmp, GMatrix ma)
        {
            Bitmap outBmp = new Bitmap(bmp);
            using (var g = Graphics.FromImage(outBmp))
            {
                g.Clear(System.Drawing.Color.White);
            }
            //for (int y = 0; y < bmp.Height; y++)
            //{
            //    for (int x = 0; x < bmp.Width; x++)
            //    {
            //        var pix = bmp.GetPixel(x, y);
            //        var res = m.dot(new GMatrix(new double[] { x, y, 1 }, 3, 1));
            //        var zt = res.storage[2][0];
            //        var xt = res.storage[0][0] / zt;
            //        var yt = res.storage[1][0] / zt;

            //        if (xt > 0 && yt > 0)
            //        {
            //            if (xt < bmp.Width && yt < bmp.Height)
            //            {
            //                outBmp.SetPixel((int)xt, (int)yt, pix);
            //            }
            //        }
            //    }
            //}

            var mi = GMatrix.Inverse3x3(ma);
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {                    
                    var res = mi.dot(new GMatrix(new double[] { x, y, 1 }, 3, 1));
                    var zt = res.storage[2][0];
                    var xt = (int)(res.storage[0][0] / zt);
                    var yt = (int)(res.storage[1][0] / zt);

                    if (xt > 0 && yt > 0 && xt < bmp.Width && yt < bmp.Height)
                    {
                        var pix = bmp.GetPixel(xt, yt);
                        outBmp.SetPixel(x,y, pix);                        
                    }
                }
            }
            return outBmp;
        }

        

        public void DrawEpl(PointFloat[] leftPts, PointSide side, PointFloat[] rightPts, Mat left, Mat right)
        {
            const int PTS_SIZE = 1;
            for (var i = 0; i < leftPts.Length; i++)
            {                
                var pts = leftPts[i];
                var rpts = rightPts[i];
                var clr = colors[i];
                CvInvoke.Rectangle(left, new System.Drawing.Rectangle((int)(pts.X- PTS_SIZE), (int)(pts.Y+ PTS_SIZE), PTS_SIZE*2, PTS_SIZE*2), clr, 2);             
            }
            const int EPPTS_SIZE = 2;
            for (var i = 0; i < leftPts.Length; i++)
            {
                if (i >= onChecks.Length) continue;
                if (!onChecks[i]) continue;
                var pts = leftPts[i];
                var rpts = rightPts[i];
                var clr = colors[i];
                CvInvoke.Rectangle(left, new System.Drawing.Rectangle((int)(pts.X- EPPTS_SIZE), (int)(pts.Y- EPPTS_SIZE), EPPTS_SIZE*2, EPPTS_SIZE*2), clr, 2);
                var gm = GetEpLineABC(pts, side, F);
                //var gm = F.dot(new GMatrix(new double[3, 1] { { pts.X }, { pts.Y }, { 1  } }));
                DrawEpl(right, gm, clr, rpts);
            }
        }

        public class LineSlop
        {
            public double a { get; protected set; }
            public double b { get; protected set; }
            public double c { get; protected set; }
            public LineSlop(double aa, double bb, double cc)
            {
                a = aa;
                b = bb;
                c = cc;
            }
        }
        private LineSlop GetEpLineABC(PointFloat pts, PointSide side, GMatrix f)
        {
            if (side == PointSide.Right)
            {
                f = f.tranpose();
            }
            var gm = f.dot(new GMatrix(new double[3, 1] { { pts.X }, { pts.Y }, { 1 } }));
            var ms = gm.storage;
            var a = ms[0][0];
            var b = ms[1][0];
            var c = ms[2][0];
            return new LineSlop(a, b, c);
                
        }
        
        private void DrawEpl(Mat img, LineSlop m, MCvScalar clr, PointFloat rpts)
        {
            
            var a = m.a;
            var b = m.b;
            var c = m.c;
            //Console.WriteLine(m);
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
                //Console.WriteLine($"Drawinga {p1.X},{p1.Y}      {p2.X},{p2.Y}     {sum}");
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
                //Console.WriteLine($"Drawingb {p1.X},{p1.Y}      {p2.X},{p2.Y}         {sum}");
            }
        }
    }
    
}
