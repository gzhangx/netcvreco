using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netCvLib
{
    public interface IDriver
    {
        Task Track(netCvLib.VidLoc.RealTimeTrackLoc realTimeTrack);
        void Stop();
    }
}
