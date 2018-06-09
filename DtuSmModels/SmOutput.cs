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
        public enum outputType
        {
            linkFlowTimeSeries = 0,
            nodeVolume = 1,
            GlobalVolumen = 2,
            outletFlowTimeSeries = 3,
            //data series
            //accumulated only
            //
        }



        public List<double> data;
        public double accumulated;
        public outputType type;

        public SmOutput()
        {
            data = new List<double>();
        }

        public void updateData(double x)
        {
            data.Add(x);
        }

        internal void clear()
        {
            data.Clear();
        }
    }
}
