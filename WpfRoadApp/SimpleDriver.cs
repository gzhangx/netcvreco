using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netCvLib;
using System.Net;
using System.IO;

namespace WpfRoadApp
{
    public class SimpleDriver : IDriver
    {
        public bool sendCommand;
        public static string url = "http://192.168.168.100";
        protected int endPos;
        public void SetEndPos(int pos)
        {
            endPos = pos;
        }

        public void Stop()
        {
            Console.WriteLine("Stoping");            
            Drive($"/steer/100/400");            
        }
        public void Track(VidLoc.RealTimeTrackLoc realTimeTrack)
        {

            if (realTimeTrack.NextPos > endPos - 5)
            {
                Console.WriteLine($"next pos {realTimeTrack.NextPos}/{endPos}, skipping");
                Stop();
                return;
            }
            if (Math.Abs(realTimeTrack.vect.X) > 1)
            {
                var dir = -(int)(realTimeTrack.vect.X );
                Console.WriteLine($"driving {dir} {realTimeTrack.vect.X.ToString("0.0")}");
                Drive($"steer/{dir}/160");
            }
        }

        public void Drive(string data)
        {
            if (!sendCommand) return;
            try
            {
                // Create a request for the URL.   
                WebRequest request = WebRequest.Create(
               $"{url}/{data}");
                // If required by the server, set the credentials.  
                request.Credentials = CredentialCache.DefaultCredentials;
                // Get the response.  
                WebResponse response = request.GetResponse();
                // Display the status.  
                Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                // Get the stream containing content returned by the server.  
                Stream dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.  
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.  
                string responseFromServer = reader.ReadToEnd();
                // Display the content.  
                Console.WriteLine(responseFromServer);
                // Clean up the streams and the response.  
                reader.Close();
                response.Close();
            } catch (Exception exc)
            {
                Console.WriteLine($"Driver Failure {exc.Message}");
            }
        }
    }
}
