using netCvLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netCvLibTest
{
    class Program
    {
        static void Main(string[] args)
        {
            NumberGrouper gp = new NumberGrouper(10);
            var res = gp.Process(new int[] { 1,1,1,2,2,-9,9,8,6});
            Console.WriteLine(res);
        }
    }
}
