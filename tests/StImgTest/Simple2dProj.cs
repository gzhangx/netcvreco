using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StImgTest
{
    public class Simple2dProj
    {
        protected float _camPlanZ, _camPointToPlan;
        protected float camPointZ;

        public float rotX { get; set; }
        public float rotY { get; set; }     
        public Simple2dProj(float camPlanZ, float camPointToPlan = 100)
        {            
            _camPointToPlan = camPointToPlan;
            setCamPlanZ(camPlanZ);
        }

        public void setCamPlanZ(float z)
        {
            _camPlanZ = z;
            camPointZ = _camPlanZ + _camPointToPlan;
        }

        protected float translateOne(float x, float z)
        {
            float zdiff = camPointZ - z;
            return (x * _camPointToPlan / zdiff);
        }



        float mul(MCvPoint3D32f pt, double[] mat)
        {
            return (float)((pt.X * mat[0]) + (pt.Y * mat[1]) + (pt.Z * mat[2]));
        }
        MCvPoint3D32f rot(MCvPoint3D32f pt, double[][] mat)
        {
            return new MCvPoint3D32f(
                mul(pt, mat[0]),
                mul(pt, mat[1]),
                mul(pt, mat[2])
                );
        }

        public Point proj(MCvPoint3D32f opt)
        {
            var ptx = rot(opt, new double[][]
            {
                new double[]{1,              0,               0 },
                new double[]{0, Math.Cos(rotX), -Math.Sin(rotX) },
                new double[]{0, Math.Sin(rotX),  Math.Cos(rotX)},
            });
            var pty = rot(ptx, new double[][]
            {                
                new double[]{ Math.Cos(rotY), 0, Math.Sin(rotY) },
                new double[]{              0, 1,              0 },
                new double[]{-Math.Sin(rotY), 0, Math.Cos(rotY)},
            });
            return new Point((int)translateOne(pty.X, pty.Z), (int)translateOne(pty.Y, pty.Z));
        }
    }
}
