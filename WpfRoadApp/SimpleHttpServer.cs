namespace WpfRoadApp
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Unosquare.Labs.EmbedIO;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Net;

    public class SimpleHttpServer
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public void Start()
        {
            var url = "http://+:80/";

            // Our web server is disposable.
            var server = new WebServer(url, Unosquare.Labs.EmbedIO.Constants.RoutingStrategy.Regex);
            {
                //server.RegisterModule(new LocalSessionModule());

                // Here we setup serving of static files
               
                
                // We don't need to add the line below. The default document is always index.html.
                //server.Module<Modules.StaticFilesWebModule>().DefaultDocument = "index.html";
                server.RegisterModule(new StaticFilesModule("../netcvreco/web/drive-app/build"));
                // The static files module will cache small files in ram until it detects they have been modified.
                server.Module<StaticFilesModule>().UseRamCache = false;
                server.Module<StaticFilesModule>().DefaultExtension = ".html";

                server.RegisterModule(new WebApiModule());
                server.Module<WebApiModule>().RegisterController<SteerController>();
                // Once we've registered our modules and configured them, we call the RunAsync() method.
                server.RunAsync();
            }
        }

        public class SteerController: WebApiController {

            public SteerController(IHttpContext context) :base(context) { }
            public class resp
            {
                public string msg { get; set; }
                public uint ok { get; set; }
            }

            [WebApiHandler(Unosquare.Labs.EmbedIO.Constants.HttpVerbs.Get, "/api/r/{id}")]
            public async Task<bool> GetR(int id)
            {
                var max = 15;
                var center = 90;
                var low = center - max;
                var high = center + max;
                Console.Write($"GOT {id} => ");
                if (id > high) id = high;
                else if (id < low) id = low;
                Console.WriteLine(id);
                TrackingStats.CmdRecorder.AddCommandInfo(new CommandInfo
                {
                    Command = "R",
                    CommandParam = id,
                });
                var res = await SimpleDriver.comm.Turn(id);
                return this.JsonResponse(new resp { msg = res.Err, ok = res.OK });
            }

            [WebApiHandler(Unosquare.Labs.EmbedIO.Constants.HttpVerbs.Get, "/api/d/{id}")]
            public async Task<bool> Drive(int id)
            {
                Console.WriteLine("Drive " + id);
                if(id != 0)
                {
                    if (!TrackingStats.CmdRecorder.Inited)
                    {
                        TrackingStats.CmdRecorder.Init();
                        await GetR(90);
                    }
                }
                var res = await SimpleDriver.comm.Drive(id);
                TrackingStats.CmdRecorder.AddCommandInfo(new CommandInfo
                {
                    Command = "D",
                    CommandParam = id,
                });
                if (id == 0)
                {
                    TrackingStats.CmdRecorder.Stop();
                }
                return this.JsonResponse(new resp { msg = res.Err, ok = res.OK });
            }


            static bool cancelReplay = true;
            [WebApiHandler(Unosquare.Labs.EmbedIO.Constants.HttpVerbs.Get, "/api/replay")]
            public async Task<bool> Replay()
            {
                Console.WriteLine("Replay");
                cancelReplay = false;
                TrackingStats.CmdRecorder.Load();

                foreach (var cmd in TrackingStats.CmdRecorder.Commands)
                {
                    if (cancelReplay) break;
                    DateTime now = DateTime.Now;
                    if (cmd.Command == "D")
                    {
                        var res = await SimpleDriver.comm.Drive(cmd.CommandParam);
                        Console.WriteLine($"D {cmd.CommandParam} doe with {res.OK} {res.Err} {cancelReplay}");
                    }else if (cmd.Command == "R")
                    {
                        var res = await SimpleDriver.comm.Turn(cmd.CommandParam);
                        Console.WriteLine($"R {cmd.CommandParam} doe with {res.OK} {res.Err} {cancelReplay}");
                    }
                    if (cmd.timeMs > 0)
                    {
                        double spent = DateTime.Now.Subtract(now).TotalMilliseconds;
                        if (spent < cmd.timeMs)
                        {
                            if (cancelReplay) break;
                            var delay = (int)(cmd.timeMs - spent);
                            Console.WriteLine($"Speeping {delay} {cancelReplay}");
                            await Task.Delay(delay);
                        }
                    }
                }

                if (cancelReplay)
                {
                    await SimpleDriver.comm.Drive(0);
                }
                return this.JsonResponse(new resp { msg = "OK", ok = 0 });
            }

            [WebApiHandler(Unosquare.Labs.EmbedIO.Constants.HttpVerbs.Get, "/api/cancelReplay")]
            public bool CancelReplay()
            {
                Console.WriteLine("Canceled");
                cancelReplay = true;
                return true;
            }
        }
    }
}