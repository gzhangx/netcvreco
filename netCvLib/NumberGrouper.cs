using System.Collections.Generic;
using System.Linq;

namespace netCvLib
{
    public class NumberGrouper
    {
        public class NumberRange
        {
            public int High { get; set; }
            public int Low { get; set; }
            public double Value { get; set; }
            public double Percentage { get; set; }
            public override string ToString()
            {
                return $"{Low} {High} pct={Percentage} val={Value}";
            }
        }
        protected int Range = 100;
        protected int[] RangeSpec;
        protected int trim;
        public double ThreadShold = 0.2; //20%
        public NumberGrouper(int range)
        {
            Range = range;
            RangeSpec = new int[range*2];
            trim = range / 2 / 5;
        }
        public List<NumberRange> Process(int[] numbers)
        {
            int max = Range * 2 - 1;
            foreach (var n in numbers)
            {
                var un = n + Range;
                if (n < 0) un = 0;
                else if (n > max) un = max;
                RangeSpec[un]++;
            }

            
            int end = max - trim;
            double total = 0;
            for (int i = trim; i < end; i ++)
            {
                total += RangeSpec[i];
            }

            List<NumberRange> ranges = new List<NumberRange>();
            for (int step = 1; step < Range / 2; step+=2)
            {                
                for (int i = trim; i < end; i += step)
                {
                    if (i + step > end) break;
                    double curTotal = 0;
                    for (int j = 0; j < step; j++)
                    {
                        curTotal += RangeSpec[i + j];
                    }
                    if (curTotal > 0)
                    {
                        var low = i - Range;
                        ranges.Add(new NumberRange
                        {
                            High = low + step,
                            Low = low,
                            Value = curTotal,
                            Percentage = curTotal / total
                        });
                    }
                }
            }

            return ranges.OrderByDescending(r => r.Percentage).ToList();
        }
    }
}
