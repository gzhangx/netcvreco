using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixTest
{
    public class svd
    {
        public class SvdResult
        {
            public double[,] u { get; set; }
            public double[] q { get; set; }
            public double[,] v { get; set; }
            public SvdResult(double[,] u, double[]q, double[,]v)
            {
                this.u = u;
                this.q = q;
                this.v = v;
            }
        }
        /** 
         * https://raw.githubusercontent.com/danilosalvati/svd-js/master/src/svd.js
         * SVD procedure as explained in "Singular Value Decomposition and Least Squares Solutions. By G.H. Golub et al."
 *
 * This procedure computes the singular values and complete orthogonal decomposition of a real rectangular matrix A:
 *    A = U * diag(q) * V(t), U(t) * U = V(t) * V = I
 * where the arrays a, u, v, q represent A, U, V, q respectively. The actual parameters corresponding to a, u, v may
 * all be identical unless withu = withv = {true}. In this case, the actual parameters corresponding to u and v must
 * differ. m >= n is assumed (with m = a.length and n = a[0].length)
 *
 *  @param a {Array} Represents the matrix A to be decomposed
 *  @param [withu] {bool} {true} if U is desired {false} otherwise
 *  @param [withv] {bool} {true} if U is desired {false} otherwise
 *  @param [eps] {Number} A constant used in the test for convergence; should not be smaller than the machine precision
 *  @param [tol] {Number} A machine dependent constant which should be set equal to B/eps0 where B is the smallest
 *    positive number representable in the computer
 *
 *  @returns {Object} An object containing:
 *    q: A vector holding the singular values of A; they are non-negative but not necessarily ordered in
 *      decreasing sequence
 *    u: Represents the matrix U with orthonormalized columns (if withu is {true} otherwise u is used as
 *      a working storage)
 *    v: Represents the orthogonal matrix V (if withv is {true}, otherwise v is not used)
 *
 */
        public static SvdResult SVD(double[,] a, bool withu = true, bool withv = true) {
            // Define default parameters
            double eps;
            double tol;
            eps = Math.Pow(2, -52);
            tol = 1e-64 / eps;


            // Householder's reduction to bidiagonal form

            //var n = a[0].length;
            //var m = a.length;
            var n = a.GetLength(1);
            var m = a.GetLength(0);

            if (m < n)
            {
                throw new Exception("Invalid matrix: m < n");
            }

            int i, j, k, l = 0, l1;

            double s, f, g, h, y, c, x, z;
            g = 0;
            x = 0;
            double[] e = new double[n];


            var u = new double[m, n];
            var v = new double[n, n];
            var q = new double[n];

            // Initialize u
            //for (i = 0; i < m; i++){u[i] = new Array(n).fill(0)}

            // Initialize v
            //for (i = 0; i < n; i++) v[i] = new Array(n).fill(0)


            // Initialize q
            //q = new Array(n).fill(0)

            // Copy array a in u
            for (i = 0; i < m; i++)
            {
                for (j = 0; j < n; j++)
                {
                    u[i, j] = a[i, j];
                }
            }

            for (i = 0; i < n; i++)
            {
                e[i] = g;
                s = 0;
                l = i + 1;
                for (j = i; j < m; j++)
                {
                    s += Math.Pow(u[j, i], 2);
                }
                if (s < tol)
                {
                    g = 0;
                }
                else
                {
                    f = u[i, i];
                    g = f < 0 ? Math.Sqrt(s) : -Math.Sqrt(s);
                    h = f * g - s;
                    u[i, i] = f - g;
                    for (j = l; j < n; j++)
                    {
                        s = 0;
                        for (k = i; k < m; k++)
                        {
                            s += u[k, i] * u[k, j];
                        }
                        f = s / h;
                        for (k = i; k < m; k++)
                        {
                            u[k, j] = u[k, j] + f * u[k, i];
                        }
                    }
                }
                q[i] = g;
                s = 0;
                for (j = l; j < n; j++)
                {
                    s += Math.Pow(u[i, j], 2);
                }
                if (s < tol)
                {
                    g = 0;
                }
                else
                {
                    f = u[i, i + 1];
                    g = f < 0 ? Math.Sqrt(s) : -Math.Sqrt(s);
                    h = f * g - s;
                    u[i, i + 1] = f - g;
                    for (j = l; j < n; j++)
                    {
                        e[j] = u[i, j] / h;
                    }
                    for (j = l; j < m; j++)
                    {
                        s = 0;
                        for (k = l; k < n; k++)
                        {
                            s += u[j, k] * u[i, k];
                        }
                        for (k = l; k < n; k++)
                        {
                            u[j, k] = u[j, k] + s * e[k];
                        }
                    }
                }
                y = Math.Abs(q[i]) + Math.Abs(e[i]);
                if (y > x)
                {
                    x = y;
                }
            }

            // Accumulation of right-hand transformations
            if (withv)
            {
                for (i = n - 1; i >= 0; i--)
                {
                    if (g != 0)
                    {
                        h = u[i, i + 1] * g;
                        for (j = l; j < n; j++)
                        {
                            v[j, i] = u[i, j] / h;
                        }
                        for (j = l; j < n; j++)
                        {
                            s = 0;
                            for (k = l; k < n; k++)
                            {
                                s += u[i, k] * v[k, j];
                            }
                            for (k = l; k < n; k++)
                            {
                                v[k, j] = v[k, j] + s * v[k, i];
                            }
                        }
                    }
                    for (j = l; j < n; j++)
                    {
                        v[i, j] = 0;
                        v[j, i] = 0;
                    }
                    v[i, i] = 1;
                    g = e[i];
                    l = i;
                }
            }

            // Accumulation of left-hand transformations
            if (withu)
            {
                for (i = n - 1; i >= 0; i--)
                {
                    l = i + 1;
                    g = q[i];
                    for (j = l; j < n; j++)
                    {
                        u[i, j] = 0;
                    }
                    if (g != 0)
                    {
                        h = u[i, i] * g;
                        for (j = l; j < n; j++)
                        {
                            s = 0;
                            for (k = l; k < m; k++)
                            {
                                s += u[k, i] * u[k, j];
                            }
                            f = s / h;
                            for (k = i; k < m; k++)
                            {
                                u[k, j] = u[k, j] + f * u[k, i];
                            }
                        }
                        for (j = i; j < m; j++)
                        {
                            u[j, i] = u[j, i] / g;
                        }
                    }
                    else
                    {
                        for (j = i; j < m; j++)
                        {
                            u[j, i] = 0;
                        }
                    }
                    u[i, i] = u[i, i] + 1;
                }
            }

            // Diagonalization of the bidiagonal form
            eps = eps * x;
            bool testConvergence;
            for (k = n - 1; k >= 0; k--)
            {
                for (var iteration = 0; iteration < 50; iteration++)
                {
                    // test-f-splitting
                    testConvergence = false;
                    for (l = k; l >= 0; l--)
                    {
                        if (Math.Abs(e[l]) <= eps)
                        {
                            testConvergence = true;
                            break;
                        }
                        if (Math.Abs(q[l - 1]) <= eps)
                        {
                            break;
                        }
                    }

                    if (!testConvergence)
                    { // cancellation of e[l] if l>0
                        c = 0;
                        s = 1;
                        l1 = l - 1;
                        for (i = l; i < k + 1; i++)
                        {
                            f = s * e[i];
                            e[i] = c * e[i];
                            if (Math.Abs(f) <= eps)
                            {
                                break; // goto test-f-convergence
                            }
                            g = q[i];
                            q[i] = Math.Sqrt(f * f + g * g);
                            h = q[i];
                            c = g / h;
                            s = -f / h;
                            if (withu)
                            {
                                for (j = 0; j < m; j++)
                                {
                                    y = u[j, l1];
                                    z = u[j, i];
                                    u[j, l1] = y * c + (z * s);
                                    u[j, i] = -y * s + (z * c);
                                }
                            }
                        }
                    }

                    // test f convergence
                    z = q[k];
                    if (l == k)
                    { // convergence
                        if (z < 0)
                        {
                            // q[k] is made non-negative
                            q[k] = -z;
                            if (withv)
                            {
                                for (j = 0; j < n; j++)
                                {
                                    v[j, k] = -v[j, k];
                                }
                            }
                        }
                        break; // break out of iteration loop and move on to next k value
                    }

                    // Shift from bottom 2x2 minor
                    x = q[l];
                    y = q[k - 1];
                    g = e[k - 1];
                    h = e[k];
                    f = ((y - z) * (y + z) + (g - h) * (g + h)) / (2 * h * y);
                    g = Math.Sqrt(f * f + 1);
                    f = ((x - z) * (x + z) + h * (y / (f < 0 ? (f - g) : (f + g)) - h)) / x;

                    // Next QR transformation
                    c = 1;
                    s = 1;
                    for (i = l + 1; i < k + 1; i++)
                    {
                        g = e[i];
                        y = q[i];
                        h = s * g;
                        g = c * g;
                        z = Math.Sqrt(f * f + h * h);
                        e[i - 1] = z;
                        c = f / z;
                        s = h / z;
                        f = x * c + g * s;
                        g = -x * s + g * c;
                        h = y * s;
                        y = y * c;
                        if (withv)
                        {
                            for (j = 0; j < n; j++)
                            {
                                x = v[j, i - 1];
                                z = v[j, i];
                                v[j, i - 1] = x * c + z * s;
                                v[j, i] = -x * s + z * c;
                            }
                        }
                        z = Math.Sqrt(f * f + h * h);
                        q[i - 1] = z;
                        c = f / z;
                        s = h / z;
                        f = c * g + s * y;
                        x = -s * g + c * y;
                        if (withu)
                        {
                            for (j = 0; j < m; j++)
                            {
                                y = u[j, i - 1];
                                z = u[j, i];
                                u[j, i - 1] = y * c + z * s;
                                u[j, i] = -y * s + z * c;
                            }
                        }
                    }
                    e[l] = 0;
                    e[k] = f;
                    q[k] = x;
                }
            }

            // Number below eps should be zero
            for (i = 0; i < n; i++)
            {
                if (q[i] < eps) q[i] = 0;
            }

            return new SvdResult(u, q, v);
        }


    }
}
