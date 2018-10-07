using DisplayLib;
using Emgu.CV;
using netCvLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfRoadApp
{
    public interface RVReporter
    {
        void ShowMat(Mat mat);
        void Recorded();
        void Tracked();
    }
    public class RoadVideoCapture
    {
        protected GZVideoCapture gv;
        bool inGrab = false;
        RVReporter reporter;
        WindowShiftCompare cmpWin;
        StdVideoSaver videoSaver;
        private object videoSaverLock = new object();
        public RoadVideoCapture(WindowShiftCompare swin, RVReporter rpt, int ind = 0)
        {
            reporter = rpt;
            cmpWin = swin;
            gv = new GZVideoCapture(grabbed, ind);
        }

        public void StartRecording(string name = "orig")
        {
            lock (videoSaverLock)
            {
                videoSaver = new StdVideoSaver(name, cmpWin);
            }
        }

        public void StartRecordingNew()
        {
            StartRecording("newvid");
        }
        public void EndRecording()
        {
            lock (videoSaverLock)
            {
                videoSaver = null;
            }
        }

        protected void grabbed(Mat mat)
        {
            if (inGrab)
            {
                //Console.WriteLine("Skipping frame");
                return;
            }
            inGrab = true;

            reporter.ShowMat(mat);
            ShiftVecDector.ResizeToStdSize(mat);
            if (TrackingStats.CamTrackEnabled)
            {
                cmpWin.CamTracking(mat).ContinueWith(t =>
                {
                    inGrab = false;
                    reporter.Tracked();
                });
                return;
            }
            else
            {
                lock (videoSaverLock)
                {
                    if (videoSaver != null)
                    {
                        videoSaver.SaveVid(mat);
                        reporter.Recorded();
                    }
                }
                inGrab = false;
            }
        }
    }
}
