namespace WpfRoadApp
{
    using System;
    using System.Net.Http;
    using Unosquare.Labs.EmbedIO;
    using Unosquare.Labs.EmbedIO.Modules;

    public class SimpleHttpServer
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public void Start()
        {
            var url = "http://localhost:80/";

            // Our web server is disposable.
            var server = new WebServer(url, Unosquare.Labs.EmbedIO.Constants.RoutingStrategy.Regex);
            {
                //server.RegisterModule(new LocalSessionModule());

                // Here we setup serving of static files
               
                server.RegisterModule(new WebApiModule());
                server.Module<WebApiModule>().RegisterController<SteerController>();
                // We don't need to add the line below. The default document is always index.html.
                //server.Module<Modules.StaticFilesWebModule>().DefaultDocument = "index.html";
                server.RegisterModule(new StaticFilesModule("../netcvreco/web/drive-app/build"));
                // The static files module will cache small files in ram until it detects they have been modified.
                server.Module<StaticFilesModule>().UseRamCache = false;
                server.Module<StaticFilesModule>().DefaultExtension = ".html";

                // Once we've registered our modules and configured them, we call the RunAsync() method.
                server.RunAsync();
            }
        }

        public class SteerController: WebApiController {
           
            [WebApiHandler(Unosquare.Labs.EmbedIO.Constants.HttpVerbs.Get,"/r/{id}")]
            public string GetR(int id)
            {
                Console.WriteLine("Rotate " + id);
                return id.ToString();
            }

            [WebApiHandler(Unosquare.Labs.EmbedIO.Constants.HttpVerbs.Get, "/d/{id}")]
            public string Drive(int id)
            {
                Console.WriteLine("Drive " + id);
                return id.ToString();
            }
        }
    }
}