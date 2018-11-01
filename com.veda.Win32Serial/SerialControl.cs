using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.veda.Win32Serial
{
    public class SerialControl : IComError
    {
        W32Serial _comm = new W32Serial();
        IComApp _app;
        protected bool started = false;
        public void init(IComApp app)
        {
            started = true;
            this._app = app;
            _comm.SetErrorListener(this);
            try
            {
                _comm.Open("COM3", 9600);
                _comm.Start(app);
            }
            catch (Exception exc)
            {

            }
        }

        public void Stop()
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
            Console.Write(System.Text.ASCIIEncoding.ASCII.GetString(buf));
        }

        public void OnStart(W32Serial ser)
        {

        }
    }
}
