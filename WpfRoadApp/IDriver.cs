using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfRoadApp
{
    public interface IDriver
    {
        void SetEndPos(int pos);
        void Track(netCvLib.VidLoc.RealTimeTrackLoc realTimeTrack);
    }
}
