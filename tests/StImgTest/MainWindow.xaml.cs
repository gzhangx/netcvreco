
using Emgu.CV;
using Emgu.CV.Structure;
using netCvLib.calib3d;
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
        VideoCapture _Capture1;
        VideoCapture _Capture2;



        Image<Bgr, Byte> frame_S1;
        Image<Gray, Byte> Gray_frame_S1;
        Image<Bgr, Byte> frame_S2;
        Image<Gray, Byte> Gray_frame_S2;

        Window3dProj projWin = new Window3dProj();


        string[] images = new string[] { "0", "1", "2", "3", "4", "5", "6", "7" };
        const string imageDir = @"C:\test\netCvReco\data\images";
        com.veda.LinearAlg.CalibRect.RectifyResult calres;
        static com.veda.LinearAlg.PointFloat[] convertToPF(System.Drawing.PointF[] p)
        {
            var res = new com.veda.LinearAlg.PointFloat[p.Length];
            for (int i = 0; i < p.Length; i++)
            {
                res[i] = new com.veda.LinearAlg.PointFloat(p[i].X, p[i].Y);
            }
            return res;
        }
        public MainWindow()
        {
            InitializeComponent();
            if (false)
            {
                try
                {
                    Calib.FileRectify();
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc);
                }
            }

            var al = new List<com.veda.LinearAlg.PointFloat>();
            var ar = new List<com.veda.LinearAlg.PointFloat>();
            List<com.veda.LinearAlg.CalibRect.StereoPoints> allPts = new List<com.veda.LinearAlg.CalibRect.StereoPoints>();
            com.veda.LinearAlg.PointFloat imgSize = null;
            foreach (var iii in images)
            {
                var left = CvInvoke.Imread($"{imageDir}\\Left_{iii}.jpg");
                imgSize = new com.veda.LinearAlg.PointFloat(left.Width, left.Height);
                var right = CvInvoke.Imread($"{imageDir}\\Right_{iii}.jpg");
                var corl = convertToPF(netCvLib.calib3d.Calib.findConers(left.ToImage<Gray, Byte>()));
                al.AddRange(corl);
                var corr = convertToPF(netCvLib.calib3d.Calib.findConers(right.ToImage<Gray, Byte>()));
                ar.AddRange(corr);

                allPts.Add(new com.veda.LinearAlg.CalibRect.StereoPoints { Left = corl, Right = corr });
                //File.WriteAllLines($"{imageDir}\\Left_{iii}.txt", cornerToString(corl));
                //File.WriteAllLines($"{imageDir}\\Right_{iii}.txt", cornerToString(corr));
                //var ff = com.veda.LinearAlg.Calib.CalcFundm((corl), (corr));
                //Console.WriteLine(ff);

            }
            Console.WriteLine("F");
            var F = com.veda.LinearAlg.Calib.CalcFundm(al.ToArray(), ar.ToArray());
            Console.WriteLine(F);


            calres = com.veda.LinearAlg.CalibRect.Rectify(allPts, imgSize);

            projWin.Show();
            //return;
            _Capture1 = new VideoCapture(1);
            _Capture2 = new VideoCapture(0);
            //We will only use 1 frame ready event this is not really safe but it fits the purpose
            _Capture1.ImageGrabbed += ProcessFrame;
            //_Capture2.Start(); //We make sure we start Capture device 2 first
            _Capture1.Start();
            _Capture2.Start();           
        }

        Calib.CornersStepCfg firstCfg = new Calib.CornersStepCfg();
        Calib.CalibOutput calibRes = null;
        Depth dptCalc = null;
        Compute3DFromStereoCfg cfg = new Compute3DFromStereoCfg();

        bool doShoot = false;
        private void ProcessFrame(object sender, EventArgs arg)
        {
            try
            {
                if (!_Capture1.IsOpened || !_Capture2.IsOpened) return;
            }
            catch
            {
                return;
            }
            //Aquire the frames or calculate two frames from one camera
            Mat m1 = new Mat();
            _Capture1.Retrieve(m1);
            //var m1 = _Capture1.QueryFrame();
            try
            {
                frame_S1 = m1.ToImage<Bgr, Byte>();
                Gray_frame_S1 = frame_S1.Convert<Gray, Byte>();
            } catch (Exception exc)
            {
                return;
            }
            Mat m2 = new Mat();
            try
            {
                _Capture2.Retrieve(m2);
                frame_S2 = m2.ToImage<Bgr, Byte>();
                Gray_frame_S2 = frame_S2.Convert<Gray, Byte>();
            }
            catch (Exception exc)
            {
                return;
            }

            if (firstCfg.done || true)
            {
                if (calibRes == null)
                {
                    calibRes = Calib.Caluculating_Stereo_Intrinsics(firstCfg.corners_points_Left, firstCfg.corners_points_Right, frame_S1.Size);
                    Calib.Rectify(calibRes);
                }
                else
                {
                    if (dptCalc == null)
                    {
                        Calib.Rectify(calibRes);
                        dptCalc = new Depth(calibRes.Q);                        
                    }
                    var res = dptCalc.Computer3DPointsFromStereoPair(Gray_frame_S1, Gray_frame_S2, cfg);
                    if (save)
                    {
                        save = false;
                        List<String> data = new List<string>();
                        foreach(var p in res.points)
                        {
                            data.Add($"{p.X},{p.Y},{p.Z}");
                        }
                        File.WriteAllLines("points.txt", data.ToArray());
                        UIInvoke(() =>
                        {
                            info.Text = "Saved";
                        });
                    }
                    UIInvoke(() =>
                    {
                        disparityMap.Source = DisplayLib.Util.Convert(res.disparityMap.Bitmap);
                        if (show)
                        {
                            show = false;
                            projWin.SetData(res.points);                            
                        }
                    });
                }
                
            } else
            {
                var foundLine = Calib.findCorners(firstCfg, Gray_frame_S1, Gray_frame_S2, doShoot);
                if (foundLine) doShoot = false;
                Calib.DrawChessFound(frame_S1, frame_S2, firstCfg);
            }
            UIInvoke(() =>
            {
                video1.Source = DisplayLib.Util.Convert(frame_S1.Bitmap);
                video2.Source = DisplayLib.Util.Convert(frame_S2.Bitmap);
            });
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

        private void minDisp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            cfg.minDispatities = (int)minDisp.Value;
            lblMinDisp.Content = "minDisp " + cfg.minDispatities;
        }

        private void numDisp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            cfg.numDisparities = (int)((int)(numDisp.Value+1))*16;
            lblNumDisp.Content = "numDisp " + cfg.numDisparities;
        }

        private void blockSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            cfg.SAD = (int)((int)(blockSize.Value)*2)+1;
            lblBlkSize.Content = "blk " + cfg.SAD;
        }

        private void speckle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int sp = ((int)speckle.Value) * 16;
            lblSpeckle.Content = "Speckl " + sp;
            cfg.Speckle = sp;
        }

        private void speckleRange_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int sp = ((int)speckleRange.Value) * 16;
            lblSpeckleRange.Content = "SpRange " + sp;
            cfg.SpeckleRange = sp;
        }

        private void btnShoot_Click(object sender, RoutedEventArgs e)
        {
            doShoot = true;
        }
    }
    
}
