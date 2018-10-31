using com.veda.Win32Serial;
using DisplayLib;
using netCvLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CrashTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ISaveVideoReport
    {
        public static SerialControl comm = new SerialControl();
        public MainWindow()
        {
            InitializeComponent();
            comm.init(new SimpleComApp());

            var videoSaver = new StdVideoSaver("testtestimg", this, true);
            var gv = new GZVideoCapture(mat=>
            {
                Console.Write("!");
            }, 0);
            Task.Run( async () =>
            {
                while(true)
                {
                    if (comm.WriteQueueLength > 0)
                    {
                        Console.Write("-");
                        await Task.Delay(10);
                        continue;
                    }
                    Console.Write("+");
                    await comm.WriteComm("R10");
                    
                }

            });
        }

        public void InfoReport(string s, bool isLR)
        {
        }

        public void ShowProg(int i, string s)
        {
        }
    }
}
