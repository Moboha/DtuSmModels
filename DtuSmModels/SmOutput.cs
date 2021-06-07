using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtuSmModels
{
    public class SmOutput
    {
        public string name;
        public Connection con;
        public Outlet outletx;
        public Node nodex;//node index
        public DerivedValue derived;
        public enum OutputType
        {
            linkFlowTimeSeries = 0,
            nodeVolume = 1,
            GlobalVolumen = 2,
            outletFlowTimeSeries = 3,
            nodeWaterLevel = 4,
            WQmassFlux = 5,
            //WQnodeConc = 6,//data series
            //accumulated only
            //
        }

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


        public List<double> data;
        //public double accumulated;
        public OutputType type;

        public StatType statType = StatType.deterministic;

        public SmOutput()
        {
            data = new List<double>();
        }

        public void updateData(double x)
        {
            data.Add(x);
        }

        public void clear()
        {
            data.Clear();
        }
    }
}
