using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static com.veda.Win32Serial.SerialControlWin32;

namespace com.veda.Win32Serial
{
    public class SerialControl
    {
        protected SerialPort serial;

        private List<W32Serial.SerWriteInfo> _writeQueue = new List<W32Serial.SerWriteInfo>();
        private object _writeQueueLock = new object();
        private bool running = false;

        protected virtual void PreProcessQueue(List<W32Serial.SerWriteInfo> queue)
        {
            if (queue.Count > 1)
            {
                var last = queue.Last();
                List<W32Serial.SerWriteInfo> bad = new List<W32Serial.SerWriteInfo>();
                queue.ForEach(q =>
                {
                    if (q != last)
                    {
                        if (last.canOverRide(q))
                            bad.Add(q);
                    }
                });
                if (bad.Count > 0)
                {
                    Console.WriteLine("Removing duplicates =============>" + bad.Count);                                        
                    bad.ForEach(wi =>
                    {
                        queue.Remove(wi);
                        if (wi.Done != null) Task.Run(() => { try { wi.Done(0, "no buf"); } catch { }; });
                    });
                }                
            }
        }
        private Thread CreateSerialWriteThread()
        {
            var thread = new Thread(() =>
            {
                try
                {
                    var inWrite = false;
                    while (running)
                    {
                        if (serial == null || !serial.IsOpen) break;
                        W32Serial.SerWriteInfo wi = null;
                        lock (_writeQueueLock)
                        {
                            if (!running) break;
                            if (_writeQueue.Count == 0)
                                Monitor.Wait(_writeQueueLock);
                            PreProcessQueue(_writeQueue);
                            while (inWrite)
                            {
                                Thread.Sleep(100);
                                Console.WriteLine("in write wait");
                            }                            
                            wi = _writeQueue[0];
                            _writeQueue.RemoveAt(0);
                        }
                        //if (wi.Done != null) try { wi.Done(0, "no buf"); } catch { };
                        //continue;
                        Action<uint,string> notifyDone = (code, msg) =>
                        {
                            if (wi.Done != null) Task.Run(() => { try { wi.Done(0, "no buf"); } catch { }; });
                        };
                        if (wi == null || wi.buf == null || wi.buf.Length == 0)
                        {
                            notifyDone(0, "no buf");
                            continue;
                        }
                        inWrite = true;

                        serial.Write(wi.buf, 0, wi.buf.Length);
                        try
                        {
                           notifyDone(0, comApp.waitSerialResponse());
                        }
                        catch { }
                        inWrite = false;
                    }
                }
                catch (Exception exc)
                {
                    if (running)
                        Console.WriteLine(exc.Message);
                    else
                        Console.WriteLine("serial write thread done");
                }
                Console.WriteLine("serial write thread end!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            });
            thread.Name = "SerialWriteThread";
            return thread;
        }
        private Thread CreateSerialReadThread(IComApp app)
        {
            var thread = new Thread(() =>
            {
                byte[] buf = new byte[2048];
                try
                {
                    while (running)
                    {
                        var readLen = serial.BaseStream.Read(buf, 0, buf.Length);
                        if (readLen <= 0)
                        {
                            Console.WriteLine("end of stream");
                            break;
                        }
                        var data = new byte[readLen];
                        Array.Copy(buf, data, readLen);
                        app.OnData(data);
                    }
                }
                catch (Exception exc)
                {
                    if (running)
                    {
                        Console.WriteLine(exc.Message);
                        Restart();
                    }
                    else
                        Console.WriteLine("serial read thread done");
                }
            });
            thread.Name = "SerialReadThread";
            return thread;
        }
        private Func<string> restartFunc;
        private IComApp comApp;
        protected string init(IComApp app, int baudRate)
        {
            this.comApp = app;
            if (serial != null) return "Already Open";
            restartFunc = () =>
            {
                try
                {
                    var portName = app.PortName;
                    SerialPortFixer.Execute(portName);
                    serial = new SerialPort(portName, baudRate);
                    running = true;
               
                    serial.Open();
                } catch (Exception exc)
                {
                    Console.WriteLine(exc.Message);
                    Thread.Sleep(2000);
                    Restart();
                }
                var writeThread = CreateSerialWriteThread();
                writeThread.Start();


                CreateSerialReadThread(app).Start();
                return "";
            };
            return restartFunc();
        }

        public int WriteQueueLength
        {
            get
            {
                lock(_writeQueueLock)
                {
                    return _writeQueue.Count;
                }
            }
        }

        public string Restart()
        {
            Stop();
            return restartFunc();
        }
        public void WriteComm(W32Serial.SerWriteInfo wi)
        {
            if (wi == null) return;
            lock (_writeQueueLock)
            {
                _writeQueue.Add(wi);
                Monitor.Pulse(_writeQueueLock);
            }
        }
        public Task<SerialRes> WriteComm(string s, W32Serial.SerWriteInfoCmpareInfo ovInf = null)
        {
            return WriteComm(System.Text.ASCIIEncoding.ASCII.GetBytes(s), ovInf);
        }
        public Task<SerialRes> WriteComm(byte[] buff, W32Serial.SerWriteInfoCmpareInfo ovInf = null)
        {
            TaskCompletionSource<SerialRes> ts = new TaskCompletionSource<SerialRes>();

            WriteComm(new W32Serial.SerWriteInfo
            {
                OverRideInfo = ovInf,
                buf = buff,
                Done = (stat, err) =>
                {
                    ts.SetResult(new SerialRes { OK = stat, Err = err });
                }
            });

            return ts.Task;
        }
        public virtual void Stop()
        {
            running = false;
            WriteComm(new W32Serial.SerWriteInfo());
            if (serial != null)
            {
                try { serial.Close(); } catch { }
            }
            serial = null;
        }
    }

    public class SerialControlWin32 : IComError
    {
        W32Serial _comm = new W32Serial();
        IComApp _app;
        protected bool started = false;
        protected string init(IComApp app, string portName = "COM3", int baudRate = 9600)
        {
            var pns = System.IO.Ports.SerialPort.GetPortNames();
            if (pns.Length == 0)
            {
                return "No ports found";
            }
            started = true;
            this._app = app;
            _comm.SetErrorListener(this);
            try
            {
                _comm.Open(portName, baudRate);
                _comm.Start(app);
            }
            catch (Exception exc)
            {
                return exc.Message;
            }
            return "";
        }

        public virtual void Stop()
        {
            started = false;
            _comm.Close();
        }

        public class SerialRes
        {
            public uint OK { get; set; }
            public string Err { get; set; }
        }

        public int WriteQueueLength
        {
            get
            {
                return _comm.WriteQueueLength;
            }
        }
        public Task<SerialRes> WriteComm(string s)
        {
            return WriteComm(System.Text.ASCIIEncoding.ASCII.GetBytes(s));
        }
        public Task<SerialRes> WriteComm(byte[] buff)
        {
            TaskCompletionSource<SerialRes> ts = new TaskCompletionSource<SerialRes>();

            _comm.WriteComm(new W32Serial.SerWriteInfo
            {
                buf = buff,
                Done = (stat, err) =>
                {
                    ts.SetResult(new SerialRes { OK = stat, Err = err });
                }
            });

            return ts.Task;
        }

        bool inRestart = false;
        void IComError.OnError(string err, bool finished)
        {
            if (!started) return;
            if (inRestart) return;
            inRestart = true;
            Console.WriteLine(err);
            if (finished)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    init(_app);
                    inRestart = false;
                });
            }
        }
    }


    public class SimpleComApp : IComApp
    {
        public void OnData(byte[] buf)
        {
            Console.Write('~');
            Console.Write(System.Text.ASCIIEncoding.ASCII.GetString(buf));
        }

        public virtual string waitSerialResponse()
        {
            return "";
        }
        public string PortName { get; set; }
        public void OnStart(W32Serial ser)
        {

        }
    }
}
