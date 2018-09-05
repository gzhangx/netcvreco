using DisplayLib;
using Emgu.CV;
using Emgu.CV.Structure;
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
    public partial class WindowShiftCompare : Window
    {
        VideoProvider vidProvider = new VideoProvider();

        protected int image1Ind = 1, image2Ind = 1;
        bool constChecking = false;
        BitmapImage GetImageAt(int i)
        {
            return new BitmapImage(new Uri("file://" + vidProvider.GetPath(i)));
        }

        DetailsWindow detailWind = new DetailsWindow();
        public WindowShiftCompare()
        {
            detailWind.Show();
            InitializeComponent();
            slidera.Maximum = sliderb.Maximum = 805;
            //CropAll();
        }

        public void CropAll()
        {
            for (int i = 0; i <= 805; i++)
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
            for (int i = 0; i <= 805; i++)
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
            }
        }

        VidLoc.RealTimeTrackLoc realTimeTrack = new VidLoc.RealTimeTrackLoc();
        private void sliderb_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderbval != null)
            {
                image2Ind = (int)sliderb.Value;
                imageSecond.Source = GetImageAt(image2Ind);
                if (constChecking)
                {
                    vidProvider.Pos = image1Ind;
                    Mat m1 = vidProvider.GetCurMat();
                    realTimeTrack.CurPos = image2Ind;
                    VidLoc.FindObjectDown(vidProvider, m1, realTimeTrack);
                    info.Text = $"Tracked vid at ${image1Ind} cam at ${image2Ind} next point ${realTimeTrack.NextPos} ${realTimeTrack.vect}";
                }
            
                sliderbval.Text = sliderb.Value.ToString("0");
                breakAndDiff();

            }
        }

        private void sliderSteps_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (curProcessor == null) return;
            Mat res = curProcessor.ShowStepChange(allDiffs, (int)sliderSteps.Value, null);
            imageStepRes.Source = res.MatToImgSrc();


            var dv = allDiffs[(int)sliderSteps.Value];
            var dbg = curProcessor.CalculateDiffVectDbg(dv.Location.X, dv.Location.Y);

            var inputMat = curProcessor.input.Clone();
            CvInvoke.Rectangle(inputMat, dbg.SrcRect, new MCvScalar(200));
            imageFirst.Source = inputMat.MatToImgSrc();

            var cmpMat = curProcessor.compareTo.Clone();
            CvInvoke.Rectangle(cmpMat, dbg.CompareToRect, new MCvScalar(200));
            imageSecond.Source = cmpMat.MatToImgSrc();
            detailWind.SetMyImg(1, dbg.area);
            detailWind.SetMyImg(2, dbg.orig);
            Mat normed = new Mat();
            //CvInvoke.Normalize(dbg.diffMap.Mat, normed, 0, 255, Emgu.CV.CvEnum.NormType.MinMax);
            dbg.diffMap.Mat.ConvertTo(normed, Emgu.CV.CvEnum.DepthType.Cv8U, 255);
            detailWind.SetMyImg(3, normed);
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

        void breakAndDiff()
        {
            vidProvider.Pos = image1Ind;
            Mat m1 = vidProvider.GetCurMat();
            vidProvider.Pos = image2Ind;
            Mat m2 = vidProvider.GetCurMat();
            curProcessor = new ShiftVecProcessor(m1, m2);
            //Mat res = ShiftVecDector.BreakAndNearMatches(m1, m2);
            allDiffs = curProcessor.GetAllDiffVect();
            var vect = ShiftVecProcessor.calculateTotalVect(allDiffs);
            var average = allDiffs.Average(x => x.Diff);
            info.Text = "Diff Vect " + vect + " average " + average.ToString("0.00");            


            Mat res = curProcessor.ShowAllStepChange(allDiffs);
            imageThird.Source = res.MatToImgSrc();
            sliderSteps.Maximum = allDiffs.Count - 1;
        }
}
}
