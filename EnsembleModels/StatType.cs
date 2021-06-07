using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnsembleModels
{
    public enum StatType
    {
        deterministic = 0,
        mean = 1,
        std = 2,
        min = 3,
        max = 4,
        median = 5,
        accumulated = 6,
        //...
    }
}
