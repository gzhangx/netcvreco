
using static netCvLib.VidLoc;

namespace WpfRoadApp
{
    public class TrackingStats
    {
        public static CommandRecorder CmdRecorder;
        protected static RealTimeTrackLoc realTimeTrack = new RealTimeTrackLoc();
        public static bool StayAtSamePlace
        {
            get;set;
        }
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
