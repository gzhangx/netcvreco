﻿using DisplayLib;
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
        protected WindowShiftCompare cmpWin = new WindowShiftCompare();
        public MainWindow()
        {
            InitializeComponent();
            cmpWin.Show();
            this.Left = 0;
            this.Top = 0;
            cmpWin.Left = this.Left + this.Width;
            cmpWin.Top = 0;
            new Thread(() =>
            {
                fillCameras();
            }).Start();

            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        public class CamInfo
        {
            public int Id { get; set; }
            public string Name { get
                {
                    return $"Cam {Id + 1}";
                }
            }
        }
        List<CamInfo> AllCams = new List<CamInfo>();
        protected void fillCameras()
        {
            AllCams.Clear();
            for (int i = 0; i < 1; i++)
            {
                Console.WriteLine($"Tracing {i}");
                var vv = new VideoCapture(Settings.Default.CameraId);
                if (vv.IsOpened)
                {
                    AllCams.Add(new CamInfo { Id = i });
                }
                vv.Dispose();
            }
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                cmdCameras.ItemsSource = AllCams;
                if (AllCams.Count > 0)
                    cmdCameras.SelectedIndex = AllCams.Count - 1;
            }));            
        }
        private VideoCapture vid;

        private VideoWriter vw;
        private void start_Click(object sender, RoutedEventArgs e)
        {
            StartRecord();
        }

        protected void StartRecord()
        {
            if (vid == null)
            {
                if (AllCams.Count == 0)
                {
                    MessageBox.Show("No cam detected");
                    return;
                }
                recordCount = 0;
                vw = null;
                vid = new VideoCapture(cmdCameras.SelectedIndex);
                vid.ImageGrabbed += Vid_ImageGrabbed;
                var w = vid.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth);
                var h = vid.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight);
                File.Delete("test.mp4");                
                //vw = new VideoWriter("test.mp4", -1, 10, new System.Drawing.Size((int)w, (int)h), true);
            }
            vid.Start();
        }

        protected void CreateVW(int w, int h)
        {
            if (vw == null)
            {
                vw = new VideoWriter("test.mp4", VideoWriter.Fourcc('P', 'I', 'M', '1'), 10, new System.Drawing.Size(w, h), true);
            }
        }
        protected void EndRecord()
        {
            if (vid != null)
            {
                vid.Stop();
                vid.Dispose();
                if (vw != null)
                {
                    vw.Dispose();
                }
                vw = null;               
                vid = null;
            }
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
        int recordCount = 0;
        private void Vid_ImageGrabbed(object sender, EventArgs e)
        {
            if (inGrab)
            {
                //Console.WriteLine("Skipping frame");
                return;
            }
            inGrab = true;
            Thread.Sleep(50);
            Console.WriteLine($"Processiing {recordCount++}");
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (vid != null)
                {
                    var mat = vid.QueryFrame();
                    ShiftVecDector.ResizeToStdSize(mat);
                    if (chkCamTrack.IsChecked.GetValueOrDefault())
                    {
                        cmpWin.CamTracking(mat).ContinueWith(t =>
                        {
                            inGrab = false;
                        });
                        return;
                    }
                    var ims = Convert(mat.Bitmap);
                    CreateVW(mat.Width, mat.Height);
                    vw.Write(mat);
                    mainCanv.Source = ims;
                }

                inGrab = false;
            }));            
        }

        private void end_Click(object sender, RoutedEventArgs e)
        {
            EndRecord();
        }

        private void openwin_Click(object sender, RoutedEventArgs e)
        {
            if (cmpWin.IsVisible)
            {
                cmpWin.Hide();
                openwin.Content = "Open Shift Compare";
            }
            else
            {
                cmpWin.Show();
                openwin.Content = "Hide Shift Compare";
            }
        }

        private void processToStdSize_Click(object sender, RoutedEventArgs e)
        {
            processToStdSize.IsEnabled = false;
            var vidSrc = txtVideoSource.Text;
            new Thread(() =>
            {
                VideoUtil.SaveVideo(@"test.mp4", mat =>
                {
                    ShiftVecDector.ResizeToStdSize(mat);
                    //Util.Rot90(mat, Util.RotType.CW);
                    return mat;
                }, (ind, all) =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        processToStdSize.Content = $"{ind}/{all}";
                        if (ind >= all -1)
                        {
                            processToStdSize.Content = "Process Video";
                            processToStdSize.IsEnabled = true;
                        }
                    }));
                }, vidSrc);

                VideoProvider vidProvider = new VideoProvider(vidSrc);
                Mat prevMat = null;
                List<string> lines = new List<string>();
                for (int i = 0; i < vidProvider.Total; i++)
                {
                    vidProvider.Pos = i;
                    var mat = vidProvider.GetCurMat();
                    if (prevMat != null)
                    {
                        var diff = VidLoc.CompDiff(prevMat, mat);
                        lines.Add($"{diff.Vector.X} {diff.Vector.Y} {diff.Diff}");
                    }
                    prevMat = mat;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        processToStdSize.Content = $"vid {i}/{vidProvider.Total}";
                        if (i>= vidProvider.Total - 1)
                        {
                            processToStdSize.Content = "Process Video";
                            processToStdSize.IsEnabled = true;
                        }
                    }));
                }
                File.WriteAllLines($"{vidSrc}\\vect.txt", lines);
            }).Start();
        }

        private void chkSendCmd_Checked(object sender, RoutedEventArgs e)
        {
            if (cmpWin != null)
            {
                cmpWin.driver.sendCommand = chkSendCmd.IsChecked.GetValueOrDefault();
            }
        }

        private void chkCamTrack_Click(object sender, RoutedEventArgs e)
        {
            if (chkCamTrack.IsChecked.GetValueOrDefault())
            {
                cmpWin.LoadOrig();
                start.IsEnabled = false;
                StartRecord();
            }else
            {
                start.IsEnabled = true;
                EndRecord();
            }
        }
    }
}
