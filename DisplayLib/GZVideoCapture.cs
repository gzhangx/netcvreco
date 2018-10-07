using Emgu.CV;
using log4net;
using System;


namespace DisplayLib
{
    public class GZVideoCapture : IDisposable
    {
        ILog Logger = LogManager.GetLogger("mainwin");
        private object vidLock = new object();
        public int W { get; protected set; }
        public int H { get; protected set; }
        protected VideoCapture vid;
        public GZVideoCapture(Action<Mat> grabAction, int ind = 0)
        {
            //DsDevice[] _SystemCamereas = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            //WebCams = new Video_Device[_SystemCamereas.Length];
            vid = new VideoCapture(ind);            
            W = (int)vid.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth);
            H = (int)vid.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight);
            vid.ImageGrabbed += (sender,e)=>
            {
                if (vid != null)
                {
                    using (Mat mat = new Mat())
                    {
                        lock (vidLock)
                        {
                            if (vid != null) //mat = vid.QueryFrame();
                                vid.Retrieve(mat);
                        }
                        if (mat == null)
                        {
                            return;
                        }
                        grabAction(mat);
                    }
                }
            };
            vid.Start();
        }

        public void Dispose()
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
            }
        }
    }
}
