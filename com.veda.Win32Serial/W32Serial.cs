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

namespace com.veda.Win32Serial
{
    public interface IComApp
    {
        void OnStart(W32Serial ser);
        void OnData(byte[] buf);
    }

    public interface IComError
    {
        void OnError(string err, bool finished);
    }
    public class W32Serial
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

        protected IComError onErr;
        public void SetErrorListener(IComError err)
        {
            onErr = err;
        }
        public void Open(string comPortName = "COM3", int baudRate = 128000)
        {
            if (m_hCommPort != IntPtr.Zero) return;
            SerialPort comm = new SerialPort();
            comm.BaudRate = baudRate;
            m_hCommPort = GWin32.CreateFile(comPortName,
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
                if (onErr != null)
                {
                    onErr.OnError("Open com failed " + errorMessage, true);
                }
                throwWinErr("Open com failed ");
            }

            const uint EV_RXCHAR = 1, EV_TXEMPTY = 4;
            if (!GWin32.SetCommMask(m_hCommPort, EV_RXCHAR | EV_TXEMPTY))
            {
                if (onErr != null)
                {
                    onErr.OnError("Failed to Set Comm Mask", true);
                }
                throwWinErr("Failed to Set Comm Mask");
            }

            GWin32.DCB dcb = new GWin32.DCB();
            dcb.DCBLength = (uint)Marshal.SizeOf(dcb);
            if (!GWin32.GetCommState(m_hCommPort, ref dcb))
            {
                if (onErr != null)
                {
                    onErr.OnError("CSerialCommHelper : Failed to Get Comm State", true);
                }
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
                if (onErr != null)
                {
                    onErr.OnError("CSerialCommHelper : Failed to Get Comm State", true);
                }
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
            threadStarted = false;
        }

        protected void SetTimeout(uint tm = GWin32.INFINITE)
        {
            GWin32.COMMTIMEOUTS commTimeouts = new GWin32.COMMTIMEOUTS();
            commTimeouts.ReadIntervalTimeout = tm;
            GWin32.SetCommTimeouts(m_hCommPort, ref commTimeouts);
        }

        public class SerWriteInfo
        {
            public byte[] buf { get; set; }
            public Action<uint, string> Done { get; set; }
        }
        private List<SerWriteInfo> _writeQueue = new List<SerWriteInfo>();
        private object _writeQueueLock = new object();

        private static void ResetOverlapped(NativeOverlapped ovo)
        {
            ovo.OffsetHigh = 0;
            ovo.OffsetLow = 0;
            ovo.EventHandle = IntPtr.Zero;
            ovo.InternalHigh = IntPtr.Zero;
            ovo.InternalLow = IntPtr.Zero;
        }

        public void Start(IComApp app)
        {            
            GWin32.PurgeComm(m_hCommPort, 0x0004 | 0x0008);
            app.OnStart(this);
            if (_thread != null) return;
            threadStarted = true;
            new Thread(() =>
            {
                bool inWrite = false;
                NativeOverlapped ov = new System.Threading.NativeOverlapped();
                while (threadStarted)
                {
                    if (m_hCommPort == IntPtr.Zero) break;
                    SerWriteInfo wi = null;
                    lock(_writeQueueLock)
                    {
                        if (_writeQueue.Count == 0)
                            Monitor.Wait(_writeQueueLock);
                        while(inWrite)
                        {
                            Thread.Sleep(100);
                            Console.WriteLine("in write wait");
                        }
                        wi = _writeQueue[0];
                        _writeQueue.RemoveAt(0);
                    }
                    //if (wi.Done != null) try { wi.Done(0, "no buf"); } catch { };
                    //continue;
                    if (wi == null || wi.buf == null || wi.buf.Length == 0)
                    {
                        if (wi.Done != null) try { wi.Done(0, "no buf"); } catch { };
                        continue;
                    }
                    inWrite = true;
                    ResetOverlapped(ov);
                    if (!GWin32.WriteFileEx(m_hCommPort, wi.buf, (uint)wi.buf.Length, ref ov, (uint err, uint b, ref NativeOverlapped c) =>
                    {
                        if (err != 0)
                        {
                            if (wi.Done != null) try { wi.Done(err, "Write come done " + err); } catch { };
                        }
                        else if (wi.Done != null) try { wi.Done(b, "OK"); } catch { }
                        inWrite = false;
                    }))
                    {
                        //Console.WriteLine("failed write comm " + getWinErr());
                        if (wi.Done != null) try { wi.Done(255, "failed write comm " + getWinErr()); } catch { };
                        inWrite = false;
                    }
                    // IOCompletion routine is only called once this thread is in an alertable wait state.
                    gwait(); //only with thread
                }
                Console.WriteLine("Com Write Queue done");
            }).Start();
            _thread = new Thread(() =>
            {
                var tbuf = new byte[2048];
                var buf1 = new byte[1];
                NativeOverlapped ovo = new System.Threading.NativeOverlapped();
                try
                {
                    while (threadStarted)
                    {
                        SetTimeout(0); //set always wait
                        GWin32.SetLastError(0);
                        ResetOverlapped(ovo);
                        GWin32.ReadFileEx(m_hCommPort, buf1, (uint)buf1.Length, ref ovo, (uint err, uint len, ref NativeOverlapped ovoo) =>
                        {
                            if (err != 0)
                            {
                                Console.WriteLine("read got err " + err);
                            }
                            else
                            {
                                SetTimeout();
                                uint numRead;
                                NativeOverlapped ov = new System.Threading.NativeOverlapped();
                                ov.EventHandle = GWin32.CreateEvent(IntPtr.Zero, true, false, null);
                                
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
                                    app.OnData(buf);
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
                    if (onErr != null)
                    {
                        onErr.OnError("InvalidOperationException " + iv.Message, true);
                        return;
                    }
                    Console.WriteLine("InvalidOperationException " + iv.Message);
                }
                Close();
                onErr.OnError("thread done", true);
                Console.WriteLine("thread done");
            });
            _thread.Start();
        }

        protected void gwait()
        {
            GWin32.SleepEx(GWin32.INFINITE, true);
        }
        public int WriteQueueLength
        {
            get
            {
                lock (_writeQueueLock)
                {
                    return _writeQueue.Count;
                }
            }
        }
        public void WriteComm(SerWriteInfo wi)
        {
            if (wi == null) return;
            lock(_writeQueueLock)
            {
                _writeQueue.Add(wi);
                Monitor.Pulse(_writeQueueLock);
            }
            /*
            new Thread(() =>
            {
                if (m_hCommPort == IntPtr.Zero) return;
                NativeOverlapped ov = new System.Threading.NativeOverlapped();
                if (!GWin32.WriteFileEx(m_hCommPort, buf, (uint)buf.Length, ref ov, (uint err, uint b, ref NativeOverlapped c) =>
                {
                    if (err != 0) reject("Write come done " + err);
                    resolve(b);
                }))
                {
                    reject("failed write comm " + getWinErr());
                }
                // IOCompletion routine is only called once this thread is in an alertable wait state.
                gwait(); //only with thread
            }).Start();
            */
        }
    }
}
