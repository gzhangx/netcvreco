using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netCvLib
{
    public interface IDriver
    {
        void SetEndPos(int pos);
        void Track(netCvLib.VidLoc.RealTimeTrackLoc realTimeTrack);
        void Stop();
    }
}
