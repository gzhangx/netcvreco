using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.veda.LinearAlg
{
    public class Util
    {
        public static double[] SVBkSb(GMatrix u, double[] w, GMatrix v, double[] b)
        {            
            int m = u.rows, n = u.cols;
            double[] x = new double[n];
            double[] temp = new double[n];
            for (int j = 0; j <n; j++)
            {
                double s = 0;
                if (w[j] != 0)
                {
                    for (var i = 0; i < m; i++) s += u.storage[i][j] * b[i];
                    s /= w[j];
                }
                temp[j] = s;
            }
            for (var j = 0; j < n; j++)
            {
                double s = 0.0;
                for (var jj = 0; jj < n; jj++) s += v.storage[j][jj] * temp[jj];
                x[j] = s;
            }
            return x;
        }
    }
}
