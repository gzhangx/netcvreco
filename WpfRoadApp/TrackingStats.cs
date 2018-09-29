
using static netCvLib.VidLoc;

namespace WpfRoadApp
{
    public class TrackingStats
    {
        protected static RealTimeTrackLoc realTimeTrack = new RealTimeTrackLoc();
        public static RealTimeTrackLoc RealTimeTrack
        {
            get
            {
                return realTimeTrack;
            }
        }
        public static bool CamTrackEnabled
        {
            get;set;
        }
    }
}
