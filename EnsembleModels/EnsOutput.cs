using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DtuSmModels;

namespace EnsembleModels
{
    public class EnsOutput 
    {
        public SmOutput[] dataSeries;// one output pr stat 
        public EnsembleStatistics stats;

        public string name;
        public SmOutput.OutputType hydraulicType;
        public Connection[] con;
        public Outlet[] outletx;
        public Node nodex;//node index
        public DerivedValue derived;


        public EnsOutput(EnsembleStatistics statistics)
        {
            stats = statistics;
            int NstatTypes = stats.types.Length;
            dataSeries = new SmOutput[NstatTypes];

            for (int i = 0; i < NstatTypes; i++)
            {
                dataSeries[i] = new SmOutput();
            }


        }
             
      

    }
}
