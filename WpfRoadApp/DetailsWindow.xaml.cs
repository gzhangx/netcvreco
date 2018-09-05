using DisplayLib;
using Emgu.CV;
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
using System.Windows.Shapes;

namespace WpfRoadApp
{
    /// <summary>
    /// Interaction logic for DetailsWindow.xaml
    /// </summary>
    public partial class DetailsWindow : Window
    {
        public DetailsWindow()
        {
            InitializeComponent();
        }

        public void SetMyImg(int who, Mat mat)
        {
            switch(who)
            {
                case 1:
                    img1.Source = mat.MatToImgSrc();
                    break;
                case 2:
                    img2.Source = mat.MatToImgSrc();
                    break;
                case 3:
                    img3.Source = mat.MatToImgSrc();
                    break;
                case 4:
                    img4.Source = mat.MatToImgSrc();
                    break;
            }
        }
    }
}
