using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DisplayLib
{
    public static class Util
    {
        public static Bitmap matToBitmap(Mat mat)
        {
            return mat.Bitmap;
        }
        public static BitmapImage Convert(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            src.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        public static ImageSource MatToImgSrc(this Mat mat, Action<Bitmap> draw = null)
        {
            var bmp = matToBitmap(mat);
            if (draw != null) draw(bmp);
            return Convert(bmp);
        }

        public static Mat RotateImage(Mat src, double angle)
        {
            //var src = CvInvoke.Imread(file);
            Mat dst = new Mat();
            var rot = new RotationMatrix2D(new PointF(src.Width / 2.0f, src.Height / 2.0f), angle, 1);
            CvInvoke.WarpAffine(src, dst, rot, src.Size);
            return dst;
        }

        public enum RotType
        {
            CW,
            CCW,
            R180,
        }
        public static void Rot90(Mat matImage, RotType rotflag)
        {
            //1=CW, 2=CCW, 3=180
            if (rotflag == RotType.CW)
            {
                CvInvoke.Transpose(matImage, matImage);
                CvInvoke.Flip(matImage, matImage,  Emgu.CV.CvEnum.FlipType.Horizontal); //transpose+flip(1)=CW
            }
            else if (rotflag == RotType.CCW)
            {
                CvInvoke.Transpose(matImage, matImage);
                CvInvoke.Flip(matImage, matImage,  Emgu.CV.CvEnum.FlipType.None); //transpose+flip(0)=CCW     
            }
            else if (rotflag == RotType.R180)
            {
                CvInvoke.Flip(matImage, matImage, Emgu.CV.CvEnum.FlipType.Vertical);    //flip(-1)=180          
            }            
        }
    }
}
