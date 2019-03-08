using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace netCvLib.calib3d
{
    public class Calib
    {
        const int width = 6;//9 //width of chessboard no. squares in width - 1
        const int height = 3;//6 // heght of chess board no. squares in heigth - 1
        public const int buffer_length = 100; //define the aquasition length of the buffer 

        const string saveFilePath = @"C:\test\StereoImaging\StereoImaging\";
        const string saveFileName_corners = saveFilePath + "corners.txt";
        const string saveFileName_mat = saveFilePath + "mat.txt";
        public Calib()
        {
            Random R = new Random();
            for (int i = 0; i < line_colour_array.Length; i++)
            {
                line_colour_array[i] = new Bgr(R.Next(0, 255), R.Next(0, 255), R.Next(0, 255));
            }
        }
        protected static PointF[] FindChessboardCorners(Image<Gray, Byte> image, Size patternSize, Emgu.CV.CvEnum.CalibCbType flags)
        {
            PointF[] corners = new PointF[patternSize.Width * patternSize.Height];
            GCHandle handle = GCHandle.Alloc(corners, GCHandleType.Pinned);

            bool patternFound = false;
            using (Matrix<float> pointMatrix = new Matrix<float>(corners.Length, 1, 2, handle.AddrOfPinnedObject(), 2 * sizeof(float)))
            {
                patternFound = CvInvoke.FindChessboardCorners(image, patternSize, pointMatrix, flags);
            }

            handle.Free();

            return patternFound ? corners : null;
        }

        public class CornersStepCfg
        {            
            public int buffer_savepoint = 0;
            public PointF[][] corners_points_Left = new PointF[buffer_length][];//stores the calculated points from chessboard detection Camera 1
            public PointF[][] corners_points_Right = new PointF[buffer_length][];//stores the calculated points from chessboard detection Camera 2
            public bool done = false;

            //output
            public PointF[] corners_Left = null;
            public PointF[] corners_Right = null;
        }

        /// <summary>
        /// Call with new CornesStepCfg, keep calling with images till cfg.done == true
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="Gray_frame_S1"></param>
        /// <param name="Gray_frame_S2"></param>
        public static void findCorners(CornersStepCfg cfg, Image<Gray, Byte> Gray_frame_S1, Image<Gray, Byte> Gray_frame_S2)
        {
            if (File.Exists(saveFileName_corners))
            {
                var lines = File.ReadAllLines(saveFileName_corners);
                var res = stringToCorner(lines);
                cfg.corners_points_Left = res[0];
                cfg.corners_points_Right = res[1];
                cfg.done = true;
                return;
            }

            Size patternSize = new Size(width, height); //size of chess board to be detected
            #region Saving Chessboard Corners in Buffer            

            //Find the chessboard in bothe images
            cfg.corners_Left = FindChessboardCorners(Gray_frame_S1, patternSize, Emgu.CV.CvEnum.CalibCbType.AdaptiveThresh);
            cfg.corners_Right = FindChessboardCorners(Gray_frame_S2, patternSize, Emgu.CV.CvEnum.CalibCbType.AdaptiveThresh);

            //we use this loop so we can show a colour image rather than a gray: //CameraCalibration.DrawChessboardCorners(Gray_Frame, patternSize, corners);
            //we we only do this is the chessboard is present in both images
            if (cfg.corners_Left != null && cfg.corners_Right != null) //chess board found in one of the frames?
            {
                //make mesurments more accurate by using FindCornerSubPixel
                Gray_frame_S1.FindCornerSubPix(new PointF[1][] { cfg.corners_Left }, new Size(11, 11), new Size(-1, -1), new MCvTermCriteria(30, 0.01));
                Gray_frame_S2.FindCornerSubPix(new PointF[1][] { cfg.corners_Right }, new Size(11, 11), new Size(-1, -1), new MCvTermCriteria(30, 0.01));


                {
                    //save the calculated points into an array
                    cfg.corners_points_Left[cfg.buffer_savepoint] = cfg.corners_Left;
                    cfg.corners_points_Right[cfg.buffer_savepoint] = cfg.corners_Right;
                    cfg.buffer_savepoint++;//increase buffer positon

                    //check the state of buffer
                    if (cfg.buffer_savepoint == buffer_length)
                    {
                        var saveStr = cornerToString(cfg.corners_points_Left) + cornerToString(cfg.corners_points_Right);
                        File.AppendAllText(saveFileName_corners, saveStr);
                    }
                    cfg.done = true;
                    //Show state of Buffer                        
                }


                //calibrate the delay bassed on size of buffer
                //if buffer small you want a big delay if big small delay
                //Thread.Sleep(100);//allow the user to move the board to a different position
            }
            //corners_Left = null;
            //corners_Right = null;
        }


        Bgr[] line_colour_array = new Bgr[width * height]; // just for displaying coloured lines of detected chessboard
        public void DrawChessFound(Image<Bgr, Byte> frame_S1, Image<Bgr, Byte> frame_S2, CornersStepCfg cfg)
        {
            //draw the results
            frame_S1.Draw(new CircleF(cfg.corners_Left[0], 3), new Bgr(Color.Yellow), 1);
            frame_S2.Draw(new CircleF(cfg.corners_Right[0], 3), new Bgr(Color.Yellow), 1);
            for (int i = 1; i < cfg.corners_Left.Length; i++)
            {
                //left
                frame_S1.Draw(new LineSegment2DF(cfg.corners_Left[i - 1], cfg.corners_Left[i]), line_colour_array[i], 2);
                frame_S1.Draw(new CircleF(cfg.corners_Left[i], 3), new Bgr(Color.Yellow), 1);
                //right
                frame_S2.Draw(new LineSegment2DF(cfg.corners_Right[i - 1], cfg.corners_Right[i]), line_colour_array[i], 2);
                frame_S2.Draw(new CircleF(cfg.corners_Right[i], 3), new Bgr(Color.Yellow), 1);
            }
        }
        #endregion

        static string cornerToString(PointF[][] corner)
        {
            var sb = new StringBuilder();
            sb.Append(corner.Length).Append("\r\n");
            for (var i = 0; i < corner.Length; i++)
            {
                var ci = corner[i];
                for (var j = 0; j < ci.Length; j++)
                {
                    sb.Append(ci[j].X).Append(",").Append(ci[j].Y).Append(" ");
                }
                sb.Append("\r\n");
            }
            return sb.ToString();
        }
        static List<PointF[][]> stringToCorner(string[] lines)
        {
            var res = new List<PointF[][]>();
            while (lines.Length > 0)
            {
                int len = Convert.ToInt32(lines[0]);
                var curLines = new PointF[len][];
                res.Add(curLines);
                for (int i = 0; i < len; i++)
                {
                    var line = lines[i + 1];
                    var segs = line.Split(' ');
                    var pts = new List<PointF>();
                    foreach (var seg in segs)
                    {
                        if (seg.Trim() == "") continue;
                        var ps = seg.Split(',');
                        pts.Add(new PointF(Convert.ToSingle(ps[0]), Convert.ToSingle(ps[1])));
                    }
                    curLines[i] = pts.ToArray();
                }
                lines = lines.Skip(len + 1).ToArray();
            }
            return res;
        }

        public class CalibOutput
        {
            public Matrix<double> IntrinsicCam1IntrinsicMatrix = new Matrix<double>(3, 3);
            public Matrix<double> IntrinsicCam2IntrinsicMatrix = new Matrix<double>(3, 3);
            public Matrix<double> IntrinsicCam1DistortionCoeffs = new Matrix<double>(8, 1);
            public Matrix<double> IntrinsicCam2DistortionCoeffs = new Matrix<double>(8, 1);
            public Matrix<double> EX_ParamTranslationVector = new Matrix<double>(3, 1);
            public RotationVector3D EX_ParamRotationVector = new RotationVector3D();
            public Matrix<double> fundamental = new Matrix<double>(3, 3); //fundemental output matrix for StereoCalibrate
            public Matrix<double> essential = new Matrix<double>(3, 3); //essential output matrix for StereoCalibrate

            public Rectangle Rec1 = new Rectangle(); //Rectangle Calibrated in camera 1
            public Rectangle Rec2 = new Rectangle(); //Rectangle Caliubrated in camera 2
            public Matrix<double> Q = new Matrix<double>(4, 4); //This is what were interested in the disparity-to-depth mapping matrix
            public Matrix<double> R1 = new Matrix<double>(3, 3); //rectification transforms (rotation matrices) for Camera 1.
            public Matrix<double> R2 = new Matrix<double>(3, 3); //rectification transforms (rotation matrices) for Camera 1.
            public Matrix<double> P1 = new Matrix<double>(3, 4); //projection matrices in the new (rectified) coordinate systems for Camera 1.
            public Matrix<double> P2 = new Matrix<double>(3, 4); //projection matrices in the new (rectified) coordinate systems for Camera 2.
        }
        public static CalibOutput Caluculating_Stereo_Intrinsics(PointF[][] corners_points_Left,PointF[][] corners_points_Right, Size size)
        {
            if (File.Exists(saveFileName_mat))
            {
                return stringToCalibOutput(File.ReadAllLines(saveFileName_mat));
            }
            MCvPoint3D32f[][] corners_object_Points = new MCvPoint3D32f[buffer_length][]; //stores the calculated size for the chessboard
            for (int k = 0; k < corners_points_Left.Length; k++)
            {
                //Fill our objects list with the real world mesurments for the intrinsic calculations
                List<MCvPoint3D32f> object_list = new List<MCvPoint3D32f>();
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        object_list.Add(new MCvPoint3D32f(j * 20.0F, i * 20.0F, 0.0F));
                    }
                }
                corners_object_Points[k] = object_list.ToArray();
            }

            CalibOutput output = new CalibOutput();
            CvInvoke.StereoCalibrate(corners_object_Points, corners_points_Left, corners_points_Right, 
                output.IntrinsicCam1IntrinsicMatrix, 
                output.IntrinsicCam1DistortionCoeffs,
                        output.IntrinsicCam2IntrinsicMatrix, 
                        output.IntrinsicCam2DistortionCoeffs, size, 
                        output.EX_ParamRotationVector, 
                        output.EX_ParamTranslationVector, 
                        output.essential, output.fundamental, Emgu.CV.CvEnum.CalibType.Default,
                        new MCvTermCriteria(0.1e5)
                        );

            SaveOutput(output);
            return output;
        }

        static string[] matrixToString(string name, Matrix<double> mat)
        {
            List<string> res = new List<string>();
            res.Add(name);
            var sb = new StringBuilder();
            res.Add(sb.Append(mat.Cols).Append(",").Append(mat.Rows).ToString());
            sb.Length = 0;
            for (var y = 0; y < mat.Rows; y++)
            {
                for (var x = 0; x < mat.Cols; x++)
                    sb.Append(mat.Data.GetValue(y, x)).Append(",");
                res.Add(sb.ToString());
                sb.Length = 0;
            }
            return res.ToArray();
        }

        public static string[] CalibOutputToString(CalibOutput output)
        {
            var c1 = matrixToString("IntrinsicCam1IntrinsicMatrix", output.IntrinsicCam1IntrinsicMatrix);
            var c1d = matrixToString("IntrinsicCam1DistortionCoeffs", output.IntrinsicCam1DistortionCoeffs);
            var c2 = matrixToString("IntrinsicCam2IntrinsicMatrix", output.IntrinsicCam2IntrinsicMatrix);
            var c2d = matrixToString("IntrinsicCam2DistortionCoeffs", output.IntrinsicCam2DistortionCoeffs);
            var t = matrixToString("EX_ParamTranslationVector", output.EX_ParamTranslationVector);

            var r = matrixToString("EX_ParamRotationVector", output.EX_ParamRotationVector);
            var f = matrixToString("fundamental", output.fundamental);
            var e = matrixToString("essential", output.essential);

            var res = new List<string>();
            res.AddRange(c1);
            res.AddRange(c1d);
            res.AddRange(c2);
            res.AddRange(c2d);
            res.AddRange(t);
            res.AddRange(r);
            res.AddRange(f);
            res.AddRange(e);
            return res.ToArray();
        }
        public static CalibOutput stringToCalibOutput(string[] lines)
        {
            CalibOutput output = new CalibOutput();
            int pos = 0;


            output.IntrinsicCam1IntrinsicMatrix = stringToMatrix(lines, ref pos);
            output.IntrinsicCam1DistortionCoeffs = stringToMatrix(lines, ref pos);
            output.IntrinsicCam2IntrinsicMatrix = stringToMatrix(lines, ref pos);
            output.IntrinsicCam2DistortionCoeffs = stringToMatrix(lines, ref pos);
            output.EX_ParamTranslationVector = stringToMatrix(lines, ref pos);
            var rot = stringToMatrix(lines, ref pos);
            var rotData = new double[3];
            for (int i = 0; i < 3; i++)
            {
                rotData[i] = rot.Data[i, 0];
            }
            output.EX_ParamRotationVector = new RotationVector3D(rotData);
            output.fundamental = stringToMatrix(lines, ref pos);
            output.essential = stringToMatrix(lines, ref pos);
            return output;
        }
        static Matrix<double> stringToMatrix(string[] lines, ref int pos)
        {
            var name = lines[pos++];
            var colRowStr = lines[pos++];
            var colRow = colRowStr.Split(',');
            var w = Convert.ToInt32(colRow[0]);
            var h = Convert.ToInt32(colRow[1]);
            var data = new double[h, w];
            for (var y = 0; y < h; y++)
            {
                var curLine = lines[pos + y];
                var curData = curLine.Split(',');
                for (var x = 0; x <w; x++)
                {
                    data[y, x] = Convert.ToInt32(curData[x]);
                }
            }
            pos += h;
            return new Matrix<double>(data);
        }
        private static void SaveOutput(CalibOutput output)
        {
            var lines = CalibOutputToString(output);
            File.WriteAllLines(saveFileName_mat, lines);
            //Toolbox.XmlSerialize(IntrinsicCam1IntrinsicMatrix).Save(saveFilePath + "IntrinsicMatrix1.xml");
            //Toolbox.XmlSerialize(IntrinsicCam1DistortionCoeffs).Save(saveFilePath + "DistortionCoeffs1.xml");

            //Toolbox.XmlSerialize(IntrinsicCam2IntrinsicMatrix).Save(saveFilePath + "IntrinsicMatrix2.xml");
            //Toolbox.XmlSerialize(IntrinsicCam2DistortionCoeffs).Save(saveFilePath + "DistortionCoeffs2.xml");


            //Toolbox.XmlSerialize(EX_ParamTranslationVector).Save(saveFilePath + "TranslationVector.xml");
            //Toolbox.XmlSerialize(EX_ParamRotationVector).Save(saveFilePath + "RotationVector.xml");

            //Toolbox.XmlSerialize(fundamental).Save(saveFilePath + "fundamental.xml");
            //Toolbox.XmlSerialize(essential).Save(saveFilePath + "essential.xml");

        }
    }
}
