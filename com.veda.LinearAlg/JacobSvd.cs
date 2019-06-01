using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.veda.LinearAlg
{
    public class JacobSvd
    {
        protected class SvdResIntrnal
        {
            public double[] W;
            public double[] Vt;
        }
        const double DBL_EPSILON = 2.2204460492503131e-016;
        public class SvdRes
        {
            public GMatrix U;
            public double[] W;
            public GMatrix Vt;
            public GMatrix getWMat()
            {
                var mat = new GMatrix(U.rows, Vt.cols);
                int at = 0;
                for (var i = 0; i < Vt.cols; i++)
                {
                    mat.storage[i][i] = W[at++];
                }
                return mat;
            }
        }
        public static SvdRes JacobiSVD(GMatrix mat)
        {
            //m rows, n cols
            //orig mxn, U mxm, W mxn V nxn

            //W is array of size n, Vt size nxn

            var m = mat.rows;
            var n = mat.cols;
            var A = mat.tranpose().ToArray();
            var res = SVD(A,  m,n);
            return new SvdRes
            {
                U = new GMatrix(A, m, m).tranpose(),
                W = res.W,
                Vt = new GMatrix(res.Vt, n,n),
            };
        }

        static SvdResIntrnal SVD(double[] At, int m, int n)
        {
            return JacobiSVDImpl(At, m, n, Double.MinValue, DBL_EPSILON * 2);
        }
        protected static SvdResIntrnal JacobiSVDImpl(double[] At, int m, int n, 
            double minval = Double.MinValue, double eps = 2.2204460492503131e-016*10)
        {

            int n1 = n;
            //VBLAS<_Tp> vblas;
            //AutoBuffer<double> Wbuf(n);
            //double* W = Wbuf.data();
            double[] W = new double[n];
            double[] Vt = new double[n * n];
            int i, j, k, iter, max_iter = Math.Max(m, 30);
            double c, s;
            double sd;
            int astep = m;
            int vstep = n;

            for (i = 0; i < n; i++)
            {
                for (k = 0, sd = 0; k < m; k++)
                {
                    double t = At[i * astep + k];
                    sd += (double)t * t;
                }
                W[i] = sd;

                if (Vt != null)
                {
                    for (k = 0; k < n; k++)
                        Vt[i * vstep + k] = 0;
                    Vt[i * vstep + i] = 1;
                }
            }

            for (iter = 0; iter < max_iter; iter++)
            {
                bool changed = false;

                for (i = 0; i < n - 1; i++)
                    for (j = i + 1; j < n; j++)
                    {
                        // Ai = At + i * astep, *Aj = At + j * astep;
                        int Ai = i * astep, Aj = j * astep;
                        double a = W[i], p = 0, b = W[j];

                        for (k = 0; k < m; k++)
                            //p += (double)Ai[k] * Aj[k];
                            p += (double)At[Ai + k] * At[Aj + k];

                        if (Math.Abs(p) <= eps * Math.Sqrt((double)a * b))
                            continue;

                        p *= 2;
                        double beta = a - b, gamma = hypot((double)p, beta);
                        if (beta < 0)
                        {
                            double delta = (gamma - beta) * 0.5;
                            s = Math.Sqrt(delta / gamma);
                            c = (p / (gamma * s * 2));
                        }
                        else
                        {
                            c = Math.Sqrt((gamma + beta) / (gamma * 2));
                            s = (p / (gamma * c * 2));
                        }

                        a = b = 0;
                        for (k = 0; k < m; k++)
                        {
                            var t0 = c * At[Ai + k] + s * At[Aj + k];
                            var t1 = -s * At[Ai + k] + c * At[Aj + k];
                            At[Ai + k] = t0; At[Aj + k] = t1;

                            a += (double)t0 * t0; b += (double)t1 * t1;
                        }
                        W[i] = a; W[j] = b;

                        changed = true;

                        if (Vt != null)
                        {
                            //_Tp* Vi = Vt + i * vstep, *Vj = Vt + j * vstep;
                            int Vi = i * vstep, Vj = j * vstep;
                            //k = vblas.givens(Vi, Vj, n, c, s);
                            k = 0;

                            for (; k < n; k++)
                            {
                                var t0 = c * Vt[Vi + k] + s * Vt[Vj + k];
                                //var t1 = -s * Vi[k] + c * Vj[k];
                                var t1 = -s * Vt[Vi + k] + c * Vt[Vj + k];
                                //Vi[k] = t0; Vj[k] = t1;
                                Vt[Vi + k] = t0; Vt[Vj + k] = t1;
                            }
                        }
                    }
                if (!changed)
                    break;
            }

            for (i = 0; i < n; i++)
            {
                for (k = 0, sd = 0; k < m; k++)
                {
                    var t = At[i * astep + k];
                    sd += (double)t * t;
                }
                W[i] = Math.Sqrt(sd);
            }

            for (i = 0; i < n - 1; i++)
            {
                j = i;
                for (k = i + 1; k < n; k++)
                {
                    if (W[j] < W[k])
                        j = k;
                }
                if (i != j)
                {
                    //swap(W[i], W[j]);
                    swap(W, i, j);
                    if (Vt != null)
                    {
                        for (k = 0; k < m; k++)
                            //swap(At[i * astep + k], At[j * astep + k]);
                            swap(At, i * astep + k, j * astep + k);

                        for (k = 0; k < n; k++)
                            //swap(Vt[i * vstep + k], Vt[j * vstep + k]);
                            swap(Vt, i * vstep + k, j * vstep + k);
                    }
                }
            }

            //for (i = 0; i < n; i++)
            //    _W[i] = W[i];

            //if (Vt == null)
            //    return;

            //RNG rng(0x12345678);
            var rng = new Random(0x12345678);
            for (i = 0; i < n1; i++)
            {
                sd = i < n ? W[i] : 0;

                for (int ii = 0; ii < 100 && sd <= minval; ii++)
                {
                    // if we got a zero singular value, then in order to get the corresponding left singular vector
                    // we generate a random vector, project it to the previously computed left singular vectors,
                    // subtract the projection and normalize the difference.
                    double val0 = (1.0/ m);
                    for (k = 0; k < m; k++)
                    {
                        var val = (rng.Next() & 256) != 0 ? val0 : -val0;
                        At[i * astep + k] = val;
                    }
                    for (iter = 0; iter < 2; iter++)
                    {
                        for (j = 0; j < i; j++)
                        {
                            sd = 0;
                            for (k = 0; k < m; k++)
                                sd += At[i * astep + k] * At[j * astep + k];
                            double asum = 0;
                            for (k = 0; k < m; k++)
                            {
                                var t = (At[i * astep + k] - sd * At[j * astep + k]);
                                At[i * astep + k] = t;
                                asum += Math.Abs(t);
                            }
                            asum = asum > eps * 100 ? 1 / asum : 0;
                            for (k = 0; k < m; k++)
                                At[i * astep + k] *= asum;
                        }
                    }
                    sd = 0;
                    for (k = 0; k < m; k++)
                    {
                        var t = At[i * astep + k];
                        sd += (double)t * t;
                    }
                    sd = Math.Sqrt(sd);
                }

                s = (sd > minval ? 1 / sd : 0.0);
                for (k = 0; k < m; k++)
                    At[i * astep + k] *= s;
            }

            return new SvdResIntrnal
            {
                W = W,
                Vt = Vt,
            };
        }

        protected static void swap(double[] a, int i, int j)
        {
            var temp = a[i];
            a[i] = a[j];
            a[j] = temp;
        }


        protected static double hypot(double a, double b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            if (a > b)
            {
                b /= a;
                return a * Math.Sqrt(1 + b * b);
            }
            if (b > 0)
            {
                a /= b;
                return b * Math.Sqrt(1 + a * a);
            }
            return 0;
        }

        public int VBLAS_givens(double[] a, double[] b, int n, float c, float s)
        {
            return 0;
        }

    }
}
