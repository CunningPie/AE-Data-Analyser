using AEDataAnalyzer.Correlation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AEDataAnalyzer
{
    static class PearsonCorrelation
    {
        static public double Coefficient(IEnumerable<double> ValuesX, IEnumerable<double> ValuesY)
        {
            int i = 0;
            double SumNumerator = 0, SumDenominatorX = 0, SumDenominatorY = 0;

            double meanValueX = MathSupportFunctions.MeanValue(ValuesX),
                   meanValueY = MathSupportFunctions.MeanValue(ValuesY);


            while (i < ValuesX.Count() && i < ValuesY.Count())
            {
                SumNumerator += (ValuesX.ElementAt(i) - meanValueX) * (ValuesY.ElementAt(i) - meanValueY);
                SumDenominatorX += Math.Pow((ValuesX.ElementAt(i) - meanValueX), 2);
                SumDenominatorY += Math.Pow((ValuesY.ElementAt(i) - meanValueY), 2);

                i++;
            }

            double rCoeff = SumNumerator / Math.Sqrt(SumDenominatorX * SumDenominatorY);

            return rCoeff;
        }

        // Чистая корреляционная функция без наворотов
        static public Dictionary<KeyValuePair<Wave, Wave>, double> CorrelationFunction(List<Wave> Waves, string ParamType = "")
        {
            Dictionary<KeyValuePair<Wave, Wave>, double> CorrelatedCoeffs = new Dictionary<KeyValuePair<Wave, Wave>, double>();
            List<double> ValuesX = new List<double>(), ValuesY = new List<double>();

            for (int i = 0; i < Waves.Count(); i++)
            {
                switch (ParamType)
                {
                    case "Time":
                        ValuesX = (from SensorInfo si in Waves[i].Events select si.MSec).ToList();
                        break;
                    case "Energy":
                        ValuesX = (from SensorInfo si in Waves[i].Events select si.Energy).ToList();
                        break;
                    case "Amplitude":
                        ValuesX = (from SensorInfo si in Waves[i].Events select si.Amplitude).ToList();
                        break;
                }

                foreach (Wave w in Waves.Skip(i))
                {
                    var pair = new KeyValuePair<Wave, Wave>(Waves[i], w);

                    switch (ParamType)
                    {
                        case "Time":
                            ValuesY = (from SensorInfo si in w.Events select si.MSec).ToList();
                            break;
                        case "Energy":
                            ValuesY = (from SensorInfo si in w.Events select si.Energy).ToList();
                            break;
                        case "Amplitude":
                            ValuesY = (from SensorInfo si in w.Events select si.Amplitude).ToList();
                            break;
                    }

                    CorrelatedCoeffs.Add(pair, Coefficient(ValuesX, ValuesY));
                }
            }

            return CorrelatedCoeffs;
        }
        /*
        Dictionary<KeyValuePair<Wave, Wave>, double> TimeAmplitudeCorrelationFunction( List<Wave> Waves )
        {
            Dictionary<KeyValuePair<Wave, Wave>, double> CorrelatedCoeffs = new Dictionary<KeyValuePair<Wave, Wave>, double>();
            List<double> TimeValuesX, TimeValuesY, AmplitudeValuesX, AmplitudeValuesY;

            for (int i = 0; i < Waves.Count(); i++)
            {
                TimeValuesX = (from SensorInfo si in Waves[i].Events select si.MSec).ToList();
                AmplitudeValuesX = (from SensorInfo si in Waves[i].Events select si.Amplitude).ToList();

                foreach (Wave w in Waves.Skip(i))
                {
                    var pair = new KeyValuePair<Wave, Wave>(Waves[i], w);

                    TimeValuesY = (from SensorInfo si in w.Events select si.MSec).ToList();
                    AmplitudeValuesY = (from SensorInfo si in w.Events select si.Amplitude).ToList();

                    CorrelatedCoeffs.Add(pair, PearsonCoefficient(TimeValuesX, TimeValuesY) *
                        PearsonCoefficient(AmplitudeValuesX, AmplitudeValuesY));
                }
            }

            return CorrelatedCoeffs;
        }

        private Dictionary<KeyValuePair<Wave, Wave>, double> MultMixedCorrelation( List<Wave> Waves)
        {
            Dictionary<KeyValuePair<Wave, Wave>, double> CorrelatedCoeffs = new Dictionary<KeyValuePair<Wave, Wave>, double>();

            for (int i = 0; i < Waves.Count(); i++)
                for (int j = i; j < Waves.Count(); j++)
                {
                    double r = 1;

                    foreach (var coeffs in ListCoeffs.Values)
                    {
                        r *= coeffs[new KeyValuePair<Wave, Wave>(Waves[i], Waves[j])];
                    }

                    CorrelatedCoeffs.Add(new KeyValuePair<Wave, Wave>(Waves[i], Waves[j]), r);
                }

            return CorrelatedCoeffs;
        }
        
        private Dictionary<KeyValuePair<Wave, Wave>, double> SumMixedCorrelation( List<Wave> Waves )
        {
            Dictionary<KeyValuePair<Wave, Wave>, double> CorrelatedCoeffs = new Dictionary<KeyValuePair<Wave, Wave>, double>();

            for (int i = 0; i < Waves.Count(); i++)
                for (int j = i; j < Waves.Count(); j++)
                {
                    double r = 0;

                    foreach (var coeffs in ListCoeffs.Values)
                    {
                        r += Math.Abs(coeffs[new KeyValuePair<Wave, Wave>(Waves[i], Waves[j])]);
                    }

                    CorrelatedCoeffs.Add(new KeyValuePair<Wave, Wave>(Waves[i], Waves[j]), r);
                }

            return CorrelatedCoeffs;
        }
        */
    }
}
