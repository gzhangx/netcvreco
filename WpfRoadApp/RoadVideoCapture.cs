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
        Task ShowMat(Mat mat);
        void Recorded();
        Task Tracked();
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

        public void StartRecording(string name = "orig", bool saveAsMp4 = false)
        {
            lock (videoSaverLock)
            {
                videoSaver = new StdVideoSaver(name, cmpWin, saveAsMp4);
            }
        }

        public void StartRecordingNew(bool saveMp4)
        {
            StartRecording("newvid", saveMp4);
        }
        public void EndRecording()
        {
            lock (videoSaverLock)
            {
                if (videoSaver != null) videoSaver.StopRecording();
                videoSaver = null;
            }
        }

        void SaveVideo(Mat mat)
        {
            lock (videoSaverLock)
            {
                if (videoSaver != null)
                {
                    videoSaver.SaveVid(mat);
                    reporter.Recorded();
                }
            }
        }

        protected void grabbed(Mat matOrig)
        {
            if (inGrab)
            {
                //Console.WriteLine("Skipping frame");
                return;
            }
            inGrab = true;

            var mat = matOrig.Clone();
            reporter.ShowMat(mat).ContinueWith(tempt =>
            {
                using (mat)
                {
                    ShiftVecDector.ResizeToStdSize(mat);
                    if (TrackingStats.CamTrackEnabled)
                    {
                        if (cmpWin.ShouldStopTracking())
                        {
                            inGrab = false;
                            reporter.Tracked();
                            return;
                        }
                        cmpWin.CamTracking(mat).ContinueWith(t =>
                        {
                            inGrab = false;
                            reporter.Tracked();
                        });
                        SaveVideo(mat);
                        return;
                    }
                    else
                    {
                        SaveVideo(mat);
                        inGrab = false;
                    }
                }
            });
            
        }
    }
}
