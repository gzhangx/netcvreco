using com.veda.Win32Serial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdurinoTest
{
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
    class Program
    {
        static void WriteStr(W32Serial ser, string str)
        {
            ser.WriteComm(ASCIIEncoding.ASCII.GetBytes(str + "\n"));
        }
        static void Main(string[] args)
        {
            W32Serial ser = new W32Serial();
            ser.Open("COM3", 9600);
            ser.Start(new Capp());
            WriteStr(ser, "D1");
            while(true)
            {
                var str = Console.ReadLine();
                WriteStr(ser, str);
            }
        }
    }
}
