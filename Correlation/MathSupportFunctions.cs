using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AEDataAnalyzer.Correlation
{
    static class MathSupportFunctions
    {
        static public double MeanValue(IEnumerable<double> Values)
        {
            if (Values.Count() > 0)
            {
                double meanValue = 0;

                foreach (double v in Values)
                    meanValue += v;

                return meanValue / Values.Count();
            }

            return 0;
        }
    }
}
