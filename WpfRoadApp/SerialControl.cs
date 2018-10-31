using com.veda.Win32Serial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfRoadApp
{
    public class SerialControl: IComError
    {
        W32Serial _comm = new W32Serial();
        IComApp _app;
        public void init(IComApp app)
        {
            this._app = app;
            _comm.SetErrorListener(this);
            try
            {
                _comm.Open("COM3", 9600);
                _comm.Start(app);
            } catch (Exception exc)
            {

            }
        }

        public class SerialRes
        {
            public uint OK { get; set; }
            public string Err { get; set; }
        }
        public Task<SerialRes> WriteComm(string s)
        {
            TaskCompletionSource<SerialRes> ts = new TaskCompletionSource<SerialRes>();

            _comm.WriteComm(new W32Serial.SerWriteInfo
            {
                buf = System.Text.ASCIIEncoding.ASCII.GetBytes(s + "\n"),
                Done = (stat, err) =>
                {
                    ts.SetResult(new SerialRes { OK = stat, Err = err });
                }
            });

            return ts.Task;
        }
        
        public async Task<SerialRes> Turn(int v)
        {
            if (v < 10) v = 10;
            if (v > 170) v = 170;
            return await WriteComm("R" + v);
        }

        public async Task<SerialRes> Drive(int v)
        {
            if (v < 0) v = 0;
            if (v > 5) v = 5;
            return await WriteComm("D" + v);
        }

        void IComError.OnError(string err, bool finished)
        {
            Console.WriteLine(err);
            if (finished)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    init(_app);
                });
            }
        }
    }
}
