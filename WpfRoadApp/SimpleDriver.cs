﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netCvLib;
using System.Net;
using System.IO;
using com.veda.Win32Serial;

namespace WpfRoadApp
{
    public class SimpleDriver : IDriver
    {
        W32Serial comm;
        public bool sendCommand;
        public static string url = "http://192.168.168.100";

        public void Stop()
        {
            Console.WriteLine("Stoping");
            Drive(0);
            //Drive($"steer/100/400");            
        }
        public void Track(VidLoc.RealTimeTrackLoc realTimeTrack)
        {

            if (realTimeTrack.ShouldStop())
            {
                //Console.WriteLine($"next pos {realTimeTrack.NextPos}/{endPos}, skipping");
                Stop();
                return;
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
                WriteComm("R" + driveDir);
            }
        }

        public void Drive(int level)
        {
            if (!sendCommand) return;
            WriteComm("D" + level);
        }

        void WriteComm(string s)
        {
            if (comm == null)
            {
                comm = new W32Serial();
                comm.Open("COM3", 9600);
                comm.Start(new Capp());
            }
            comm.WriteComm(System.Text.ASCIIEncoding.ASCII.GetBytes(s+"\n"));
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
