using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WpfRoadApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            
            log4net.Config.BasicConfigurator.Configure();
            List<Task> tasks = new List<Task>();
            for(int i = 0; i< 4;i++)
            {
                tasks.Add(ReadyTasks());
            }
            tasks.ForEach(t => t.Wait());
            Console.WriteLine("All tasks ready");
        }

        static int debugTskCount = 0;
        private Task ReadyTasks()
        {
            return Task.Run(() =>
            {
                int who = ++debugTskCount;
                Console.WriteLine($"Started {who}");
                Thread.Sleep(1000);
                Console.WriteLine($"Ending task {who}");
            });
        }
    }
}
