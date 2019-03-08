using Emgu.CV;
using Emgu.CV.Structure;
using netCvLib.calib3d;
using System;
using System.Collections.Generic;
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

        public MainWindow()
        {
            InitializeComponent();

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

            UIInvoke(() =>
            {
                video1.Source = DisplayLib.Util.Convert(frame_S1.Bitmap);
                video2.Source = DisplayLib.Util.Convert(frame_S2.Bitmap);
            });
            
            
            if (firstCfg.done)
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
                    var res = dptCalc.Computer3DPointsFromStereoPair(Gray_frame_S1, Gray_frame_S2);
                    UIInvoke(() =>
                    {
                        disparityMap.Source = DisplayLib.Util.Convert(res.disparityMap.Bitmap);
                    });
                }
                
            } else
            {
                Calib.findCorners(firstCfg, Gray_frame_S1, Gray_frame_S2);
                Calib.DrawChessFound(frame_S1, frame_S2, firstCfg);
            }
        }

        private void UIInvoke(Action act)
        {
            Dispatcher.BeginInvoke(act);
        }
    }
    
}
