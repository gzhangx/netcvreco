using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MatrixTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var r = new GMatrix(new double[,] { { 1, 2 }, { 3, 4 } , { 1, 1 } }).cross(new GMatrix(new double[,] { { 1, 1 ,1}, { 3, 4 ,1} }));
            Console.WriteLine(r);
        }
    }


    public class GMatrix
    {
        public double[,] storage { get; protected set; }
        public GMatrix(double[,] v)
        {
            storage = v;
        }
        public GMatrix tranpose()
        {
            var r = rows;
            var c = cols;
            var newStorage = new double[r,c];
            for (var i = 0; i < r; i++)
            {
                for(var j = 0; j < c;j++)
                {
                    newStorage[i, j] = storage[j, i];
                }
            }
            return new GMatrix(storage);
        }
        int cols { get { return storage.GetLength(1);  } }
        int rows { get { return storage.GetLength(0); } }

        public GMatrix cross(GMatrix m)
        {
            var r = rows;
            var mc = m.cols;
            if (r != mc) throw new InvalidOperationException($"Cross: row {r} and col {mc} must equal");
            if (cols != m.rows) throw new InvalidOperationException($"Cross: col {cols} and row {m.rows} must equal");
            var newStorage = new double[r, mc];
            var c = cols;
            for (var i = 0; i < r; i++)
            {
                for (var j = 0; j < mc; j++)
                {
                    double total = 0;
                    for (var k = 0; k < c; k++)
                    {
                        total += storage[i, k] * m.storage[k, j];
                    }
                    newStorage[i, j] = total;
                }
            }
            return new GMatrix(newStorage);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < cols;j++)
                {
                    if (j > 0) sb.Append(",");
                    sb.Append(storage[i, j]);                    
                }
                sb.Append("\n");
            }
            return sb.ToString();
        }
    }
}
