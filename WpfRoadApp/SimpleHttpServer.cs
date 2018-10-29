using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WpfRoadApp
{
    public class SimpleHttpServer
    {
        public void Start()
        {
            TcpListener lsn = new TcpListener(IPAddress.Any, 80);
            new Thread(() =>
            {
                while (true)
                {
                    var cli = lsn.AcceptTcpClient();
                    ListenSocket(cli);
                }
            }).Start();
            lsn.Start();
        }

        protected async void ListenSocket(TcpClient cli)
        {
            try
            {
                var stream = cli.GetStream();
                StreamReader sr = new StreamReader(stream);
                var firstLine = await sr.ReadLineAsync();
                stream.Close();
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }
        }
    }
}
