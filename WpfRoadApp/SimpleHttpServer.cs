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
           
            public SteerController(IHttpContext context) : base(context)
            {

            }
            public class resp
            {
                public string msg { get; set; }
            }
            public bool inProcesing = false;
            [WebApiHandler(Unosquare.Labs.EmbedIO.Constants.HttpVerbs.Get,"/api/r/{id}")]
            public async Task<bool> GetR(int id)
            {
     
                if (inProcesing) return this.JsonResponse(new resp { msg = "busy" });
                inProcesing = true;
                try
                {
                    id = id * 180 / 200;
                    Console.WriteLine("Rotate " + id);
                    if (SimpleDriver.comm.WriteQueueLength == 0)
                        await SimpleDriver.comm.Turn(id);
                    return this.JsonResponse(new resp { msg = "r " + id });
                } finally
                {
                    inProcesing = false;
                }
            }

            [WebApiHandler(Unosquare.Labs.EmbedIO.Constants.HttpVerbs.Get, "/api/d/{id}")]
            public async Task<bool> Drive(WebServer server, HttpListenerContext context, int id)
            {
                if (context == null)
                {
                    Console.WriteLine("server not ready");
                    return false;
                }
                if (inProcesing) return context.JsonResponse(new resp { msg = "busy" });
                inProcesing = true;
                try
                {
                    Console.WriteLine("Drive " + id);
                    if (SimpleDriver.comm.WriteQueueLength == 0)
                        await SimpleDriver.comm.Drive(id);
                    return context.JsonResponse(new resp { msg = "d " + id });
                }
                finally
                {
                    inProcesing = false;
                }
            }
        }
    }
}