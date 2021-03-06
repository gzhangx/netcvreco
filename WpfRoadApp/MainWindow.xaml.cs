﻿using Emgu.CV;
using log4net;
using netCvLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using WpfRoadApp.Properties;

namespace WpfRoadApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, RVReporter, IRecordStarter
    {
        ILog Logger = LogManager.GetLogger("mainwin");
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
            TrackingStats.CmdRecorder = new CommandRecorder(this);
            cmpWin.Show();
            this.Left = 0;
            this.Top = 0;
            cmpWin.Left = this.Left + this.Width;
            cmpWin.Top = 0;            


            rc = new RoadVideoCapture(cmpWin, this);
            this.Closing += MainWindow_Closing;
            cmpWin.InProcessing = processing =>
            {
                TDispatch(() => {
                    processingInd.Background = processing ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Green;
                });
            };

            cmdCameras.ItemsSource = new List<NameSel>
            {
                new NameSel { Name="VidLoc" },
                new NameSel { Name="Face" },
            };
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
            cmpWin.driver.Dispose();
            rc.Dispose();
        }
             
        private void start_Click(object sender, RoutedEventArgs e)
        {
            start.IsEnabled = false;
            StartRecord();
        }

        public void StartRecord()
        {
            recordCount = 0;            
            rc.StartRecording();
            //if (AllCams.Count == 0)
            //{
            //    MessageBox.Show("No cam detected");
            //    return;
            //}
        }

        
        public void EndRecord()
        {
            rc.EndRecording();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                start.IsEnabled = true;
            }));            
        }

        private static object imageLock = new object();
        public static BitmapImage Convert(Bitmap src)
        {
            lock (imageLock)
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
        }
        int recordCount = 0;
        
        public Task ShowMat(Mat mat)
        {
            TaskCompletionSource<bool> ts = new TaskCompletionSource<bool>();
            var cm = new Mat();
            mat.CopyTo(cm);
            TDispatch(() =>
            {
                try
                {
                    using (cm)
                    {
                        var ims = Convert(cm.Bitmap);
                        mainCanv.Source = ims;
                    }
                } finally
                {
                    ts.SetResult(true);
                }
            });
            return ts.Task;
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
                cmpWin.ProcessSliderA();
                start.IsEnabled = false;
                recordCount = 0;
                rc.StartRecordingNew(chkSaveAsMp4.IsChecked.GetValueOrDefault());
            }
            else
            {
                EndRecord();
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
                btnReset.Content = $"Record {recordCount++}";
            });
        }

        int trackCount = 0;
        Task RVReporter.Tracked()
        {
            var ts = new TaskCompletionSource<bool>();
            TDispatch(() =>
            {
                if (cmpWin.ShouldStopTracking())
                {
                    btnReset.Content = "Stop";
                    TrackingStats.CamTrackEnabled = false;
                    chkCamTrack.IsChecked = false;
                    EndRecord();
                    cmpWin.StopDrive();
                    ts.SetResult(true);
                    return;
                }
                btnReset.Content = $"Track {trackCount++}";
                ts.SetResult(false);
            });
            return ts.Task;
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            cmpWin.ResetSliderA();
        }

        private void cmdCameras_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var sel = (NameSel)(cmdCameras.SelectedValue);
            if (sel.Name == "VidLoc")
            {
                cmpWin.SetCamTrack(new VideoLockCamTrack());
            }else if (sel.Name == "Face")
            {
                cmpWin.SetCamTrack(new HarrCascadeCamTrack());
            }
        }
    }
}
