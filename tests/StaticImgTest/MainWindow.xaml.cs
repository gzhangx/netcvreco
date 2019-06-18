﻿using Emgu.CV;
using Emgu.CV.Structure;
using netCvLib.calib3d;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
using static netCvLib.calib3d.Depth;

namespace StImgTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        Image<Bgr, Byte> frame_S1;
        Image<Gray, Byte> Gray_frame_S1;
        Image<Bgr, Byte> frame_S2;
        Image<Gray, Byte> Gray_frame_S2;

        string[] images = new string[] { "7","9","17","19","47","49","57","59" };
        const string imageDir = @"C:\test\netCvReco\data\images";
        public MainWindow()
        {
            InitializeComponent();            

            foreach (var iii in images)
            {
                var left = CvInvoke.Imread($"{imageDir}\\Left_{iii}.jpg");
                var right = CvInvoke.Imread($"{imageDir}\\Right_{iii}.jpg");
                var corl = Calib.findConers(left.ToImage<Gray, Byte>());
                var corr = Calib.findConers(right.ToImage<Gray, Byte>());
                File.WriteAllLines($"{imageDir}\\Left_{iii}.txt", cornerToString(corl));
                File.WriteAllLines($"{imageDir}\\Right_{iii}.txt", cornerToString(corr));
            }
        }

        static string[] cornerToString(PointF[] corner)
        {

            var res = new string[corner.Length];
            for (var j = 0; j < corner.Length; j++)
            {
                var sb = new StringBuilder();
                sb.Append(corner[j].X.ToString("0.000")).Append(",").Append(corner[j].Y.ToString("0.000"));
                res[j] = sb.ToString();
            }


            return res;
        }
        private void UIInvoke(Action act)
        {
            Dispatcher.BeginInvoke(act);
        }

        bool save = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            save = true;
        }

        bool show = false;
        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            show = true;
        }

    }
    
}
