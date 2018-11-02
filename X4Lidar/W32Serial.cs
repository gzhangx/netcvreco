using com.veda.Win32Serial;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.veda.X4Lidar
{

    public class LidarSerialControl: SerialControl
    {
        LidarComApp app = new LidarComApp();
        public void Init(IX4Tran tran)
        {
            app.SetTran(tran);
            this.init(app, "COM3", 128000);
        }
        public void Info()
        {
            WriteComm(new byte[] { 0xA5, 0x90 });
        }

        public override void Stop()
        {
            WriteComm(new byte[] { 0xA5, 0x65 });
            base.Stop();
        }
    }

    public class LidarComApp : IComApp
    {
        IX4Tran tran;
        public void SetTran(IX4Tran trans)
        {
            tran = trans;
        }
        public void OnData(byte[] buf)
        {
            //Console.Write(System.Text.ASCIIEncoding.ASCII.GetString(buf));
            tran.Translate(buf);
        }

        public void OnStart(W32Serial ser)
        {
            ser.WriteComm(new W32Serial.SerWriteInfo
            {
                buf = new byte[] { 0xA5, 0x60 }
            });
        }
    }
    public class W32Serial1
    {
        protected Thread _thread;
        protected bool threadStarted = false;
        protected void throwWinErr(string text)
        {
            int err = Marshal.GetLastWin32Error();
            throw new Exception($"{text} {err} {getWinErr()}");
        }
        protected string getWinErr()
        {
            int err = Marshal.GetLastWin32Error();
            string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
            return errorMessage;
        }
        protected IntPtr m_hCommPort = IntPtr.Zero;
        public void Open()
        {
            if (m_hCommPort != IntPtr.Zero) return;
            SerialPort comm = new SerialPort();
            comm.BaudRate = 128000;
            m_hCommPort = GWin32.CreateFile("COM3",
               FileAccess.Read | FileAccess.Write, //GENERIC_READ | GENERIC_WRITE,//access ( read and write)
            FileShare.None, //0,    //(share) 0:cannot share the COM port                        
            IntPtr.Zero, //0,    //security  (None)                
            FileMode.Open, //OPEN_EXISTING,// creation : open_existing
            0x20000000 | 0x40000000, //FILE_FLAG_OVERLAPPED,// we want overlapped operation
            IntPtr.Zero //0// no templates file for COM port...
            );

            if (m_hCommPort == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                throwWinErr("Open com failed ");
            }

            const uint EV_RXCHAR = 1, EV_TXEMPTY = 4;
            if (!GWin32.SetCommMask(m_hCommPort, EV_RXCHAR | EV_TXEMPTY))
            {
                throwWinErr("Failed to Set Comm Mask");
            }

            GWin32.DCB dcb = new GWin32.DCB();
            dcb.DCBLength = (uint)Marshal.SizeOf(dcb);
            if (!GWin32.GetCommState(m_hCommPort, ref dcb))
            {
                throwWinErr("CSerialCommHelper : Failed to Get Comm State");
            }

            dcb.BaudRate = (uint)comm.BaudRate;
            dcb.ByteSize = (byte)comm.DataBits;
            dcb.Parity = comm.Parity;
            dcb.StopBits = comm.StopBits;
            dcb.DsrSensitivity = false;
            dcb.DtrControl = GWin32.DtrControl.Enable;
            dcb.OutxDsrFlow = false;
            dcb.OutxCtsFlow = false;
            dcb.InX = false;
            dcb.RtsControl = GWin32.RtsControl.Disable;

            dcb.Binary = true;
            if (!GWin32.SetCommState(m_hCommPort, ref dcb))
            {
                throwWinErr("CSerialCommHelper : Failed to Set Comm State");
            }


            GWin32.COMMTIMEOUTS commTimeouts = new GWin32.COMMTIMEOUTS();
            commTimeouts.ReadIntervalTimeout = 0;          // Never timeout, always wait for data.
            commTimeouts.ReadTotalTimeoutMultiplier = 0;   // Do not allow big read timeout when big read buffer used
            commTimeouts.ReadTotalTimeoutConstant = 0;     // Total read timeout (period of read loop)
            commTimeouts.WriteTotalTimeoutConstant = 0;    // Const part of write timeout
            commTimeouts.WriteTotalTimeoutMultiplier = 0;  // Variable part of write timeout (per byte)
            GWin32.SetCommTimeouts(m_hCommPort, ref commTimeouts);
        }

        public void Close()
        {
            GWin32.CloseHandle(m_hCommPort);
            m_hCommPort = IntPtr.Zero;
        }

        public void Info()
        {
            WriteComm(new byte[] { 0xA5, 0x90 });
        }

        protected void SetTimeout(uint tm = GWin32.INFINITE)
        {
            GWin32.COMMTIMEOUTS commTimeouts = new GWin32.COMMTIMEOUTS();
            commTimeouts.ReadIntervalTimeout = tm;
            GWin32.SetCommTimeouts(m_hCommPort, ref commTimeouts);
        }
        public void Start(IX4Tran tran)
        {            
            GWin32.PurgeComm(m_hCommPort, 0x0004 | 0x0008);
            WriteComm(new byte[] { 0xA5, 0x60 });
            if (_thread != null) return;
            threadStarted = true;            
            _thread = new Thread(() =>
            {
                NativeOverlapped ov = new System.Threading.NativeOverlapped();
                try
                {
                    while (threadStarted)
                    {
                        var buf1 = new byte[1];
                        SetTimeout(0); //set always wait
                        GWin32.SetLastError(0);
                        GWin32.ReadFileEx(m_hCommPort, buf1, (uint)buf1.Length, ref ov, (uint err, uint len, ref NativeOverlapped ov1) =>
                        {
                            if (err != 0)
                            {
                                Console.WriteLine("read got err " + err);
                            }
                            else
                            {
                                SetTimeout();
                                uint numRead;
                                ov.EventHandle = GWin32.CreateEvent(IntPtr.Zero, true, false, null);
                                var tbuf = new byte[2048];
                                if (!GWin32.ReadFile(m_hCommPort, tbuf, (uint)tbuf.Length, out numRead, ref ov))
                                {
                                    if (GWin32.GetLastError() == 997) //IO Pending
                                {
                                        GWin32.WaitForSingleObject(ov.EventHandle, GWin32.INFINITE);
                                    }
                                    else
                                    {
                                        Console.WriteLine("read got err " + getWinErr());
                                    }
                                    GWin32.GetOverlappedResult(m_hCommPort, ref ov, out numRead, true);
                                }
                                GWin32.CloseHandle(ov.EventHandle);
                                ov.EventHandle = IntPtr.Zero;
                                //Console.WriteLine("got data " + numRead);
                                var buf = new byte[1 + numRead];
                                buf[0] = buf1[0];
                                if (numRead > 0)
                                {
                                    Array.Copy(tbuf, 0, buf, 1, numRead);
                                    tran.Translate(buf);
                                }
                            //Console.WriteLine(BitConverter.ToString(buf));
                        }
                        });
                        var le = GWin32.GetLastError();
                        if (le != 0)
                        {
                            _thread = null;
                            Console.WriteLine("Read Error " + le);
                            break;
                        }
                        gwait();
                    }
                } catch (InvalidOperationException iv)
                {
                    _thread = null;
                    threadStarted = false;
                    Console.WriteLine("InvalidOperationException " + iv.Message);
                }
                Console.WriteLine("thread done");
            });
            _thread.Start();
        }

        public void Stop()
        {
            WriteComm(new byte[] { 0xA5, 0x65 });
        }

        protected void gwait()
        {
            GWin32.SleepEx(GWin32.INFINITE, true);
        }
        protected void WriteComm(byte[] buf)
        {
            new Thread(() =>
            {
                NativeOverlapped ov = new System.Threading.NativeOverlapped();
                if (!GWin32.WriteFileEx(m_hCommPort, buf, (uint)buf.Length, ref ov, (uint err, uint b, ref NativeOverlapped c) =>
                {
                    if (err != 0) Console.WriteLine("Write come done " + err);
                    Console.WriteLine("Write come done tran=" + b);
                }))
                {
                    Console.WriteLine("failed write comm " + getWinErr());
                }
                // IOCompletion routine is only called once this thread is in an alertable wait state.
                gwait();
            }).Start();            
        }
    }
}
