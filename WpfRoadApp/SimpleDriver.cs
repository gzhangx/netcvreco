using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netCvLib;
using System.Net;
using System.IO;
using com.veda.Win32Serial;
using static com.veda.Win32Serial.SerialControlWin32;

namespace WpfRoadApp
{

    public class DriverSerialControl : SerialControl
    {
        public void Init(IComApp app)
        {
            base.init(app, "COM5", 9600);
        }
        public async Task<SerialRes> Turn(int v)
        {
            if (v < 10) v = 10;
            if (v > 170) v = 170;
            return await WriteComm($"R{v}\n");
        }

        public async Task<SerialRes> Drive(int v)
        {
            if (v < 0) v = 0;
            if (v > 5) v = 5;
            return await WriteComm($"D{v}\n");
        }
    }
    public class SimpleDriver : IDriver, IDisposable
    {
        public static DriverSerialControl comm = new DriverSerialControl();
        public SimpleDriver()
        {
            comm.Init(new Capp());
        }
        public bool sendCommand;
        public static string url = "http://192.168.168.100";

        public void Stop()
        {
            Console.WriteLine("Stoping");
            Drive(0);
            //Drive($"steer/100/400");            
        }

        public Task Track(VidLoc.RealTimeTrackLoc realTimeTrack)
        {

            if (realTimeTrack.ShouldStop())
            {
                //Console.WriteLine($"next pos {realTimeTrack.NextPos}/{endPos}, skipping");
                Stop();
                return Task.FromResult(0);
            }
            if (Math.Abs(realTimeTrack.vect.X) > 1)
            {
                var dir = -(int)(realTimeTrack.vect.X *20);
                Console.WriteLine($"driving {dir} {realTimeTrack.vect.X.ToString("0.0")}");
                Drive(4);
                //Drive($"steer/{dir}/100");
                var driveDir = dir;
                var baseAng = 90;
                if (dir == 0) driveDir = baseAng;
                if (dir > 0) driveDir = baseAng + 20;
                if (dir < 0) driveDir = baseAng - 20;
                return comm.Turn(driveDir);
            }
            return Task.FromResult(0);
        }

        public Task Drive(int level)
        {
            if (!sendCommand) return Task.FromResult(0);
            return comm.Drive(level);
        }

        public void Dispose()
        {
            comm.Stop();
        }

        class Capp : IComApp
        {
            public void OnData(byte[] buf)
            {
                Console.Write(System.Text.ASCIIEncoding.ASCII.GetString(buf));
            }

            public void OnStart(W32Serial ser)
            {

            }
        }
        //public void Drive(string data)
        //{
        //    if (!sendCommand) return;
        //    try
        //    {
        //        // Create a request for the URL.   
        //        WebRequest request = WebRequest.Create(
        //       $"{url}/{data}");
        //        // If required by the server, set the credentials.  
        //        request.Credentials = CredentialCache.DefaultCredentials;
        //        // Get the response.  
        //        WebResponse response = request.GetResponse();
        //        // Display the status.  
        //        Console.WriteLine(((HttpWebResponse)response).StatusDescription);
        //        // Get the stream containing content returned by the server.  
        //        Stream dataStream = response.GetResponseStream();
        //        // Open the stream using a StreamReader for easy access.  
        //        StreamReader reader = new StreamReader(dataStream);
        //        // Read the content.  
        //        string responseFromServer = reader.ReadToEnd();
        //        // Display the content.  
        //        Console.WriteLine(responseFromServer);
        //        // Clean up the streams and the response.  
        //        reader.Close();
        //        response.Close();
        //    } catch (Exception exc)
        //    {
        //        Console.WriteLine($"Driver Failure {exc.Message} {data}");
        //    }
        //}
    }
}
