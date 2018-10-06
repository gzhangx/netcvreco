using DisplayLib;
using Emgu.CV;
using log4net;
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
        ILog Logger = LogManager.GetLogger("mainwin");
        StdVideoSaver videoSaver;
        public static bool DebugMode
        {
            get
            {
                return true;
            }
        }
        public static bool SaveVideoWhileDriving
        {
            get
            {
                return false;
            }
        }
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
        private object vidLock = new object();

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
                if (!TrackingStats.CamTrackEnabled)
                {
                    videoSaver = new StdVideoSaver(txtVideoSource.Text, cmpWin);
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
                Logger.Info("End recording");
                lock (vidLock)
                {
                    vid.Stop();
                    vid.Dispose();
                    vid = null;
                }
                if (vw != null)
                {
                    vw.Dispose();
                }
                vw = null;                               
            }
        }
        public static BitmapImage Convert(Bitmap src)
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
            //Thread.Sleep(50);
            Console.WriteLine($"Processiing {recordCount++}");
            //this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (vid != null)
                {
                    Mat mat = null;
                    lock (vidLock)
                    {
                        if (vid != null) mat = vid.QueryFrame();
                    }
                    if (mat == null)
                    {
                        inGrab = false;
                        return;
                    }
                    ShiftVecDector.ResizeToStdSize(mat);
                    if (TrackingStats.CamTrackEnabled)
                    {
                        cmpWin.CamTracking(mat).ContinueWith(t =>
                        {
                            inGrab = false;
                            if (cmpWin.ShouldStopTracking())
                            {
                                EndRecord();
                            }
                            if (SaveVideoWhileDriving)
                            {
                                RecordToVW(mat);
                            }
                        });
                        return;
                    }else
                    {
                        videoSaver.SaveVid(mat);
                        ShowMat(mat);
                    }
                    //RecordToVW(mat);
                }

                inGrab = false;
            }
            //));            
        }

        void RecordToVW(Mat mat)
        {
            CreateVW(mat.Width, mat.Height);
            vw.Write(mat);
            ShowMat(mat);
        }
        void ShowMat(Mat mat)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var ims = Convert(mat.Bitmap);
                mainCanv.Source = ims;
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
            return;
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
                            //processToStdSize.IsEnabled = true;
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
                        var diff = VidLoc.CompDiff(prevMat, mat, null);
                        lines.Add($"{diff.Vector.X} {diff.Vector.Y} {diff.Vector.Diff}");
                    }
                    if (prevMat != null) prevMat.Dispose();
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
                if (prevMat != null) prevMat.Dispose();
                File.WriteAllLines($"{vidSrc}\\vect.txt", lines);
            }).Start();
        }

        private void chkSendCmd_Click(object sender, RoutedEventArgs e)
        {
            if (cmpWin != null)
            {
                cmpWin.driver.sendCommand = chkSendCmd.IsChecked.GetValueOrDefault();
            }
        }

        private void chkCamTrack_Click(object sender, RoutedEventArgs e)
        {
            TrackingStats.CamTrackEnabled = chkCamTrack.IsChecked.GetValueOrDefault();
            if (TrackingStats.CamTrackEnabled)
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

        private void chkStayAtSamePlace_Click(object sender, RoutedEventArgs e)
        {
            TrackingStats.StayAtSamePlace = chkStayAtSamePlace.IsChecked.GetValueOrDefault();
        }
    }
}
