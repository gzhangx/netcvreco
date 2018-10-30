using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netCvLib
{
    public interface ICamTrackable
    {
        void CamTracking(Mat curImg, VidLoc.RealTimeTrackLoc realTimeTrack, PreVidStream vidProvider, IDriver driver, BreakDiffDebugReporter debugReporter);
    }
}
