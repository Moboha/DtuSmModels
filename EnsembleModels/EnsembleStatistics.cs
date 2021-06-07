using System.Collections.Generic;
using System.Linq;
using DtuSmModels;

namespace EnsembleModels
{
    public class EnsembleStatistics
    {
        public SmOutput.StatType[] types;


        public void SetStats(bool mean, bool std, bool min, bool max, bool median)
        {
            List<SmOutput.StatType> tempTypes = new List<SmOutput.StatType>();
            if (mean) tempTypes.Add(SmOutput.StatType.mean);
            if (std) tempTypes.Add(SmOutput.StatType.std);
            if (min) tempTypes.Add(SmOutput.StatType.min);
            if (max) tempTypes.Add(SmOutput.StatType.max);
            if (median) tempTypes.Add(SmOutput.StatType.median);


            types = tempTypes.ToArray();
        }

    }
}