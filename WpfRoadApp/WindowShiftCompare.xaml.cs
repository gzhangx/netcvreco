﻿using DisplayLib;
using Emgu.CV;
using Emgu.CV.Structure;
using log4net;
using netCvLib;
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
using System.Windows.Shapes;

namespace WpfRoadApp
{
    /// <summary>
    /// Interaction logic for WindowShiftCompare.xaml
    /// </summary>
    public partial class WindowShiftCompare : Window, BreakDiffDebugReporter
    {
        VideoProvider vidProvider = new VideoProvider("orig");
        VideoProvider vidProviderNewVid = new VideoProvider("newvid");
        public SimpleDriver driver = new SimpleDriver();
        static ILog Logger = LogManager.GetLogger("ShWin");

        protected int image1Ind = 1, image2Ind = 1;
        bool constChecking = false;
        BitmapImage GetImageAt(int i)
        {
            return GetImageAt(vidProvider, i);
        }

        BitmapImage GetImageAt(VideoProvider provider, int i)
        {
            return new BitmapImage(new Uri("file://" + provider.GetPath(i)));
        }

        public void LoadOrig()
        {
            vidProvider = new VideoProvider("orig");
        }
        DetailsWindow detailWind = new DetailsWindow();
        public WindowShiftCompare()
        {
            //detailWind.Show();
            InitializeComponent();
            slidera.Maximum = sliderb.Maximum = vidProvider.Total;
            driver.SetEndPos(vidProvider.Total);
            this.Closing += WindowShiftCompare_Closing;
            //CropAll();
        }

        private void WindowShiftCompare_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        public void CropAll()
        {
            for (int i = 0; i <= vidProvider.Total; i++)
            {
                var fname = vidProvider.GetPath(i);
                Console.WriteLine("Converting " + fname);
                var src = CvInvoke.Imread(fname);
                var dst = new Mat(src, new System.Drawing.Rectangle(420, 0, 1500 - 420, src.Height));
                CvInvoke.Imwrite(fname, dst);
            }
        }

        public void RotateAll()
        {
            for (int i = 0; i <= vidProvider.Total; i++)
            {
                var fname = vidProvider.GetPath(i);
                Console.WriteLine("Converting " + fname);
                var src = Util.RotateImage(CvInvoke.Imread(fname), -90);
                CvInvoke.Imwrite(fname, src);
            }
        }

        private void slidera_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (slideraval != null)
            {
                slideraval.Text = slidera.Value.ToString("0");
                image1Ind = (int)slidera.Value;
                imageFirst.Source = GetImageAt(image1Ind);
                if (!TrackingStats.CamTrackEnabled)
                {
                    realTimeTrack.CurPos = image1Ind;
                }
            }
        }

        protected int CurOrigVidPos
        {
            get
            {
                return realTimeTrack.CurPos;
            }
        }
        protected int TotalOrigVidLen
        {
            get
            {
                return vidProvider.Total;
            }
        }

        public bool DebugMode
        {
            get
            {
                return MainWindow.DebugMode;
            }
        }

        public bool ShouldStopTracking()
        {
            return realTimeTrack.CurPos + 5 >= vidProvider.Total;
        }
        protected VidLoc.RealTimeTrackLoc realTimeTrack = TrackingStats.RealTimeTrack;
        private void sliderb_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderbval != null)
            {
                sliderbval.Text = sliderb.Value.ToString("0");
                image2Ind = (int)sliderb.Value;

                if (chkTrackSimulation.IsChecked.GetValueOrDefault())
                {
                    vidProviderNewVid.Pos = image2Ind;
                    imageSecond.Source = GetImageAt(vidProviderNewVid, image2Ind);
                    if (vidProviderNewVid.Pos >= vidProviderNewVid.Total)
                        vidProviderNewVid.Pos = vidProviderNewVid.Total - 1;
                    var mat = vidProviderNewVid.GetCurMat();
                    CamTracking(mat);
                    return;
                }

                imageSecond.Source = GetImageAt(image2Ind);
                if (constChecking)
                {
                    vidProvider.Pos = image2Ind;
                    Mat m1 = vidProvider.GetCurMat();
                    CamTracking(m1).ContinueWith(t =>
                    {
                        TDispatch(() => { breakAndDiff(); });
                    });
                    //realTimeTrack.CurPos = image1Ind;
                    //realTimeTrack.LookAfter = 30;
                    //VidLoc.FindObjectDown(vidProvider, m1, realTimeTrack);
                    //info.Text = $"Tracked vid at ${image1Ind} cam at ${image2Ind} next point ${realTimeTrack.NextPos} ${realTimeTrack.vect}  ===> diff {realTimeTrack.diff}";
                    //slidera.Value = realTimeTrack.NextPos - 1;
                    return;
                }

                breakAndDiff();
            }
        }

        void breakAndDiff()
        {
            vidProvider.Pos = image1Ind;
            Mat m1 = vidProvider.GetCurMat();
            vidProvider.Pos = image2Ind;
            Mat m2 = vidProvider.GetCurMat();
            VidLoc.breakAndDiff(m1, m2, this);
        }
        private void sliderSteps_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (curProcessor == null) return;
            Mat res = curProcessor.ShowStepChange(allDiffs, (int)sliderSteps.Value, null);
            imageStepRes.Source = res.MatToImgSrc();

            /*
            var dv = allDiffs[(int)sliderSteps.Value];
            curProcessor.CalculateDiffVectDbg(dv.Location.X, dv.Location.Y, dbg=>
            {
                using (var inputMat = curProcessor.input.Clone())
                {
                    CvInvoke.Rectangle(inputMat, dbg.SrcRect, new MCvScalar(200));
                    imageFirst.Source = inputMat.MatToImgSrc();

                    using (var cmpMat = curProcessor.compareTo.Clone())
                    {
                        CvInvoke.Rectangle(cmpMat, dbg.CompareToRect, new MCvScalar(200));
                        imageSecond.Source = cmpMat.MatToImgSrc();
                        detailWind.SetMyImg(1, dbg.area);
                        detailWind.SetMyImg(2, dbg.orig);
                        using (Mat normed = new Mat())
                        {
                            //CvInvoke.Normalize(dbg.diffMap.Mat, normed, 0, 255, Emgu.CV.CvEnum.NormType.MinMax);
                            dbg.diffMap.Mat.ConvertTo(normed, Emgu.CV.CvEnum.DepthType.Cv8U, 255);
                            detailWind.SetMyImg(3, normed);
                        }
                    }
                }
            });     
            */       
        }

        protected ShiftVecProcessor curProcessor = null;
        protected List<DiffVect> allDiffs = null;

        private void trackBtn_Click(object sender, RoutedEventArgs e)
        {
            vidProvider.Pos = image1Ind;
            Console.WriteLine("Starting find");
            var res = VidLoc.FindInRage(vidProvider, vidProvider.GetCurMat());
            Console.WriteLine($"Done find {res.Pos} {res.diff.ToString("0.00")}");
            sliderb.Value = res.Pos;
        }

        private void chkConstTracking_Click(object sender, RoutedEventArgs e)
        {
            constChecking = chkConstTracking.IsChecked.GetValueOrDefault();
        }

        private void chkShowDetail_Click(object sender, RoutedEventArgs e)
        {
            if (chkShowDetail.IsChecked.GetValueOrDefault())
                detailWind.Show();
            else
                detailWind.Hide();
        }

        private void chkTrackSimulation_Click(object sender, RoutedEventArgs e)
        {
            vidProviderNewVid = new VideoProvider(txtSimulationDir.Text);
        }

        public Task CamTracking(Mat curImg)
        {            
            return Task.Run(() =>
            {
                VidLoc.CamTracking(curImg, realTimeTrack, vidProvider, driver, this);
                TDispatch(() =>
                {
                    if (realTimeTrack.NextPos > 0 && !TrackingStats.StayAtSamePlace)
                    {
                        image1Ind = realTimeTrack.NextPos;
                        realTimeTrack.CurPos = realTimeTrack.NextPos;
                        slidera.Value = image1Ind;
                    }

                    imageSecond.Source = MainWindow.Convert(curImg.Bitmap);
                    //realTimeTrack.CurPos = image1Ind;
                    StringBuilder sb = new StringBuilder();
                    realTimeTrack.DebugAllLooks.ForEach(p =>
                    {
                        sb.Append($" {p.Pos}={p.diff} ");
                    });
                    var text = $"Tracked vid at {image1Ind} cam at {realTimeTrack.CurPos} next point {realTimeTrack.NextPos} {realTimeTrack.vect}  ===> diff {realTimeTrack.diff} {sb.ToString()}";
                    //Console.WriteLine(text);
                    info.Text = text;

                });
            });
        }

        public void Report(Mat res, List<DiffVect> diffs, DiffVector vect, double average)
        {
            TDispatch(() =>
            {
                //info.Text = "Diff Vect " + vect + " average " + average.ToString("0.00");
                imageThird.Source = res.MatToImgSrc();
                sliderSteps.Maximum = diffs.Count - 1;
            });
        }

        public void ReportStepChanges(ShiftVecProcessor proc, List<DiffVect> allDiffs, DiffVector vect)
        {
            var average = allDiffs.Average(x => x.Diff);
            Mat res = proc.ShowAllStepChange(allDiffs);
            Report(res, allDiffs, vect, average);            
        }

        void TDispatch(Action a)
        {
            Dispatcher.BeginInvoke(new Action(a));
        }

        public void InfoReport(string s)
        {
            TDispatch(() => {
                info2.Text = s;
                Logger.Info(s);
            });
        }
    }
}
