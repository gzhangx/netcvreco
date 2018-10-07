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
    public partial class MainWindow : Window, RVReporter
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
        protected RoadVideoCapture rc;
        public MainWindow()
        {
            InitializeComponent();
            cmpWin.Show();
            this.Left = 0;
            this.Top = 0;
            cmpWin.Left = this.Left + this.Width;
            cmpWin.Top = 0;            


            rc = new RoadVideoCapture(cmpWin, this);
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
            TDispatch(() =>
            {
                cmdCameras.ItemsSource = AllCams;
                if (AllCams.Count > 0)
                    cmdCameras.SelectedIndex = AllCams.Count - 1;
            });            
        }
        private VideoCapture vid;
        private object vidLock = new object();

        private VideoWriter vw;
        private void start_Click(object sender, RoutedEventArgs e)
        {
            start.IsEnabled = false;
            StartRecord();
        }

        protected void StartRecord()
        {
            recordCount = 0;            
            rc.StartRecording();
            //if (AllCams.Count == 0)
            //{
            //    MessageBox.Show("No cam detected");
            //    return;
            //}
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
            rc.EndRecording();
            start.IsEnabled = true;
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

        void RecordToVW(Mat mat)
        {
            CreateVW(mat.Width, mat.Height);
            vw.Write(mat);
            ShowMat(mat);
        }
        public void ShowMat(Mat mat)
        {
            var cm = new Mat();
            mat.CopyTo(cm);
            TDispatch(() =>
            {
                using (cm)
                {
                    var ims = Convert(cm.Bitmap);
                    mainCanv.Source = ims;
                }
            });
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
            trackCount = 0;
            TrackingStats.CamTrackEnabled = chkCamTrack.IsChecked.GetValueOrDefault();
            if (TrackingStats.CamTrackEnabled)
            {
                cmpWin.LoadOrig();
                start.IsEnabled = false;                
            }else
            {
                start.IsEnabled = true;
            }
        }        

        void TDispatch(Action act)
        {
            Dispatcher.BeginInvoke(new Action(act));
        }
        private void chkStayAtSamePlace_Click(object sender, RoutedEventArgs e)
        {
            TrackingStats.StayAtSamePlace = chkStayAtSamePlace.IsChecked.GetValueOrDefault();
        }

        void RVReporter.Recorded()
        {
            TDispatch(() =>
            {
                processToStdSize.Content = $"Record {recordCount++}";
            });
        }

        int trackCount = 0;
        void RVReporter.Tracked()
        {
            TDispatch(() =>
            {
                if (cmpWin.ShouldStopTracking())
                {
                    processToStdSize.Content = "Stop";
                    TrackingStats.CamTrackEnabled = false;
                    chkCamTrack.IsChecked = false;
                }
                processToStdSize.Content = $"Track {trackCount++}";
            });
        }
    }
}
