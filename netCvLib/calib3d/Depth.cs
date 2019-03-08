using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netCvLib.calib3d
{
    public class Depth
    {

        //Matrix<double> IntrinsicCam1IntrinsicMatrix = new Matrix<double>(3, 3);
        //Matrix<double> IntrinsicCam2IntrinsicMatrix = new Matrix<double>(3, 3);
        //Matrix<double> IntrinsicCam1DistortionCoeffs = new Matrix<double>(8, 1);
        //Matrix<double> IntrinsicCam2DistortionCoeffs = new Matrix<double>(8, 1);
        //Matrix<double> EX_ParamTranslationVector = new Matrix<double>(3, 1);
        //RotationVector3D EX_ParamRotationVector = new RotationVector3D();
        //Matrix<double> fundamental = new Matrix<double>(3, 3); //fundemental output matrix for StereoCalibrate
        //Matrix<double> essential = new Matrix<double>(3, 3); //essential output matrix for StereoCalibrate
        //Rectangle Rec1 = new Rectangle(); //Rectangle Calibrated in camera 1
        //Rectangle Rec2 = new Rectangle(); //Rectangle Caliubrated in camera 2
        //Matrix<double> Q = new Matrix<double>(4, 4); //This is what were interested in the disparity-to-depth mapping matrix
        //Matrix<double> R1 = new Matrix<double>(3, 3); //rectification transforms (rotation matrices) for Camera 1.
        //Matrix<double> R2 = new Matrix<double>(3, 3); //rectification transforms (rotation matrices) for Camera 1.
        //Matrix<double> P1 = new Matrix<double>(3, 4); //projection matrices in the new (rectified) coordinate systems for Camera 1.
        //Matrix<double> P2 = new Matrix<double>(3, 4); //projection matrices in the new (rectified) coordinate systems for Camera 2.

        Matrix<double> Q = new Matrix<double>(4, 4); //This is what were interested in the disparity-to-depth mapping matrix
        public Depth(Matrix<double> q)
        {
            Q = q;
        }
        public class Compute3DFromStereoCfg
        {
            /*This is maximum disparity minus minimum disparity. Always greater than 0. In the current implementation this parameter must be divisible by 16.*/
            public int numDisparities = 64;

            /*The minimum possible disparity value. Normally it is 0, but sometimes rectification algorithms can shift images, so this parameter needs to be adjusted accordingly*/
            public int minDispatities = 0;

            /*The matched block size. Must be an odd number >=1 . Normally, it should be somewhere in 3..11 range*/
            public int SAD = 1;

            /*P1, P2 – Parameters that control disparity smoothness. The larger the values, the smoother the disparity. 
             * P1 is the penalty on the disparity change by plus or minus 1 between neighbor pixels. 
             * P2 is the penalty on the disparity change by more than 1 between neighbor pixels. 
             * The algorithm requires P2 > P1 . 
             * See stereo_match.cpp sample where some reasonably good P1 and P2 values are shown 
             * (like 8*number_of_image_channels*SADWindowSize*SADWindowSize and 32*number_of_image_channels*SADWindowSize*SADWindowSize , respectively).*/

            public int P1 { get { return 8 * 1 * SAD * SAD; } }
            public int P2 { get { return 32 * 1 * SAD * SAD; } }

            /* Maximum allowed difference (in integer pixel units) in the left-right disparity check. Set it to non-positive value to disable the check.*/
            public int disp12MaxDiff = 1;

            /*Truncation value for the prefiltered image pixels. 
             * The algorithm first computes x-derivative at each pixel and clips its value by [-preFilterCap, preFilterCap] interval. 
             * The result values are passed to the Birchfield-Tomasi pixel cost function.*/
            public int PreFilterCap = 0;

            /*The margin in percents by which the best (minimum) computed cost function value should “win” the second best value to consider the found match correct. 
             * Normally, some value within 5-15 range is good enough*/
            public int UniquenessRatio = 0;

            /*Maximum disparity variation within each connected component. 
             * If you do speckle filtering, set it to some positive value, multiple of 16. 
             * Normally, 16 or 32 is good enough*/
            public int Speckle = 0;

            /*Maximum disparity variation within each connected component. If you do speckle filtering, set it to some positive value, multiple of 16. Normally, 16 or 32 is good enough.*/
            public int SpeckleRange = 0;

            /*Set it to true to run full-scale 2-pass dynamic programming algorithm. It will consume O(W*H*numDisparities) bytes, 
             * which is large for 640x480 stereo and huge for HD-size pictures. By default this is usually false*/
            //Set globally for ease
            //public bool fullDP = true;
            /// <summary>
            /// Sets the state of fulldp in the StereoSGBM algorithm allowing full-scale 2-pass dynamic programming algorithm. 
            /// It will consume O(W*H*numDisparities) bytes, which is large for 640x480 stereo and huge for HD-size pictures. By default this is false
            /// </summary>
            public StereoSGBM.Mode fullDP = StereoSGBM.Mode.SGBM;
        }
        public class Computer3DPointsFromStereoPairOutput
        {
            public Image<Gray, short> disparityMap;
            public MCvPoint3D32f[] points;
        }
        /// <summary>
        /// Given the left and right image, computer the disparity map and the 3D point cloud.
        /// </summary>
        /// <param name="left">The left image</param>
        /// <param name="right">The right image</param>
        /// <param name="disparityMap">The left disparity map</param>
        /// <param name="points">The 3D point cloud within a [-0.5, 0.5] cube</param>
        public Computer3DPointsFromStereoPairOutput Computer3DPointsFromStereoPair(Image<Gray, Byte> left, Image<Gray, Byte> right, Compute3DFromStereoCfg cfg = null)
        {
            if (cfg == null) cfg = new Compute3DFromStereoCfg();
            System.Drawing.Size size = left.Size;

            Computer3DPointsFromStereoPairOutput res = new Computer3DPointsFromStereoPairOutput();
            res.disparityMap = new Image<Gray, short>(size);
            //thread safe calibration values


            
            /*Set it to true to run full-scale 2-pass dynamic programming algorithm. It will consume O(W*H*numDisparities) bytes, 
             * which is large for 640x480 stereo and huge for HD-size pictures. By default this is usually false*/
            //Set globally for ease
            //bool fullDP = true;

            using (StereoSGBM stereoSolver = new StereoSGBM(cfg.minDispatities, cfg.numDisparities, cfg.SAD, cfg.P1, cfg.P2, cfg.disp12MaxDiff, cfg.PreFilterCap, cfg.UniquenessRatio, cfg.Speckle, cfg.SpeckleRange, cfg.fullDP))
            //using (StereoBM stereoSolver = new StereoBM(Emgu.CV.CvEnum.STEREO_BM_TYPE.BASIC, 0))
            {
                //FindStereoCorrespondence
                stereoSolver.Compute(left, right, res.disparityMap);//Computes the disparity map using: 
                /*GC: graph cut-based algorithm
                  BM: block matching algorithm
                  SGBM: modified H. Hirschmuller algorithm HH08*/
                res.points = PointCollection.ReprojectImageTo3D(res.disparityMap, Q); //Reprojects disparity image to 3D space.
            }
            return res;
        }
    }
}
