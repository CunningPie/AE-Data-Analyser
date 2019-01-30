using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AEDataAnalyzer.Correlation
{
    static class SupportFunctions
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

        static public SensorInfo DeltaTime(SensorInfo A, SensorInfo B)
        {
            A.MSec = A.Time.Subtract(B.Time).TotalMilliseconds - B.MSec + A.MSec;
            return A;
        }

        //WaveA - выборка с меньшим числом элементов
        static public Wave PointSelection(Wave WaveA, Wave WaveB)
        {
            List<SensorInfo> EventsA = new List<SensorInfo>(), EventsB = new List<SensorInfo>(), ResultPoints = new List<SensorInfo>();

            foreach (SensorInfo si in WaveA.Events)
                EventsA.Add(DeltaTime(si, WaveA.Events[0]));
            foreach (SensorInfo si in WaveB.Events)
                EventsB.Add(DeltaTime(si, WaveB.Events[0]));

            for (int i = 0; i < EventsA.Count - 1; i++)
            {
                double dA = EventsA[i + 1].MSec - EventsA[i].MSec;

                Dictionary<KeyValuePair<SensorInfo, SensorInfo>, double> deltaB = new Dictionary<KeyValuePair<SensorInfo, SensorInfo>, double>();

                for (int j = ResultPoints.Count(); j < EventsB.Count() - 1; j++)
                    deltaB.Add(new KeyValuePair<SensorInfo, SensorInfo>(EventsB[j], EventsB[j + 1]), EventsB[j + 1].MSec - EventsB[j].MSec);

                List<double> Deltas = new List<double>();

                foreach (var pair in deltaB)
                    Deltas.Add(dA - pair.Value);

                KeyValuePair<SensorInfo, SensorInfo> SensorPair = (from KeyValuePair<KeyValuePair<SensorInfo, SensorInfo>, double> pair in deltaB where dA - pair.Value == Deltas.Min() select pair.Key).First();

                if (!ResultPoints.Contains(SensorPair.Key))
                    ResultPoints.Add(SensorPair.Key);

                if (!ResultPoints.Contains(SensorPair.Value))
                    ResultPoints.Add(SensorPair.Value);
            }

            return new Wave(ResultPoints, WaveA.Number);
        }
    }
}
