using DisplayLib;
using Emgu.CV;
using netCvLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using WpfRoadApp.Properties;

namespace WpfRoadApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();            
        }

        private VideoCapture vid;

        private VideoWriter vw;
        private void start_Click(object sender, RoutedEventArgs e)
        {
            if (vid == null)
            {
                vid = new VideoCapture(Settings.Default.CameraId);
                vid.ImageGrabbed += Vid_ImageGrabbed;
                var w = vid.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth);
                var h = vid.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight);
                File.Delete("test.mp4");
                vw = new VideoWriter("test.mp4", VideoWriter.Fourcc('P', 'I', 'M', '1'), 10, new System.Drawing.Size((int)w, (int)h), true);                
                //vw = new VideoWriter("test.mp4", -1, 10, new System.Drawing.Size((int)w, (int)h), true);
            }
            vid.Start();
        }

        public BitmapImage Convert(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            src.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }
        bool inGrab = false;
        private void Vid_ImageGrabbed(object sender, EventArgs e)
        {
            if (inGrab) return;
            inGrab = true;
            Thread.Sleep(200);
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                var mat = vid.QueryFrame();
                var ims = Convert(mat.Bitmap);
                vw.Write(mat);
                mainCanv.Source = ims;
                inGrab = false;
            }));            
        }

        private void end_Click(object sender, RoutedEventArgs e)
        {
            if (vid != null)
            {
                vid.Stop();
                new Thread(() =>
                {
                    Thread.Sleep(2000);
                    this.Dispatcher.BeginInvoke(new Action(() => {
                        vw.Dispose();
                    }));
                }).Start();                
            }
        }

        private void openwin_Click(object sender, RoutedEventArgs e)
        {
            new WindowShiftCompare().Show();
        }

        private void processToStdSize_Click(object sender, RoutedEventArgs e)
        {
            VideoUtil.SaveVideo(@"D:\pics\2018-08-21\IMG_1217.MOV", mat =>
            {
                ShiftVecDector.ResizeToStdSize(mat);
                Util.Rot90(mat, Util.RotType.CW);
                return mat;
            });
        }
    }
}
