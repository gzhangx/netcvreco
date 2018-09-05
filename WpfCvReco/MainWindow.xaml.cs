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
using System.ComponentModel;

namespace WpfCvReco
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int flow = 100;
        int fhigh = 255;
        const int totalImages = 432;
        bool runWorker = true;
        Thread worker;
        string GetPath(int i)
        {
            if (i < 0) i = 0;
            i = i % 432;
            return @"D:\work\gang\cur\netCvReco.run\vid" + i + ".jpg";
        }
        string GetCurrentFile()
        {
            return GetPath(who);
        }
        public MainWindow()
        {
            //VideoUtil.SaveVideo(@"D:\pics\2018-08-21\IMG_1217.MOV");
            InitializeComponent();
            low.Text = flow.ToString();
            high.Text = fhigh.ToString();
            worker = new Thread(() =>
            {
                while (runWorker)
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        who++;
                        if (who > totalImages)
                        {
                            who = who % totalImages;
                        }
                        ShowImage();
                        videoProgress.Value = who;
                    }));
                    Thread.Sleep(500);
                }
            });
            videoProgress.Maximum = totalImages;
            worker.Start();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            runWorker = false;
        }

        Bitmap matToBitmap(Mat mat)
        {
            return mat.Bitmap;
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

        ImageSource MatToImgSrc(Mat mat, Action<Bitmap> draw = null)
        {
            var bmp = matToBitmap(mat);
            if (draw != null) draw(bmp);
            return Convert(bmp);
        }
        int who = 0;
        private void prev_Click(object sender, RoutedEventArgs e)
        {
            who--;
            ShowImage();
        }

        private void ShowImage()
        {
            Mat res = RoadDetector.Detect(GetCurrentFile(), new RoadDetector.Parms { threadshold1 = flow, threadshold2 = fhigh });
            mainImage.Source = MatToImgSrc(res);
            Mat orig = CvInvoke.Imread(GetCurrentFile());
            Mat ff = new Mat();
            orig.CopyTo(ff);

            var lines = RMatFilter.FilterRoadByMean(ff);
            secondaryImage.Source = MatToImgSrc(ff, bmp=>
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    var pen = new System.Drawing.Pen(System.Drawing.Brushes.AliceBlue, 20);
                    g.DrawLine(pen, lines.leftStart, lines.leftEnd);
                    g.DrawLine(pen, lines.rightStart, lines.rightEnd);
                }
            });
            thirdImage.Source = new BitmapImage(new Uri("file://"+ GetCurrentFile()));
        }
        private void next_Click(object sender, RoutedEventArgs e)
        {
            who++;
            ShowImage();
        }

        private void low_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                flow = Int32.Parse(low.Text);
            }
            catch { }
        }

        private void high_TextChanged(object sender, TextChangedEventArgs e)
        {
            try { 
            fhigh = Int32.Parse(high.Text);
            }
            catch { }
        }

        private void videoProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            who = (int)videoProgress.Value;
        }
    }
}
