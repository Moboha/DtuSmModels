using System;
using System.Globalization;

namespace DtuSmModels
{
    //flow is calculated as positive in the direction from subsurface to surface. 

    internal class SpillingVolumeSurfaceConnection : Connection
    {
        const double EMPTY_TIME = 60; //seconds. For flow in both directions.
        const double MIN_FLOW_TO_DRAIN = -1; //m3/s. To make sure surface empties completely. Should be paramateter and dependent on catchment size. 
        double volx;//max volume in the sub surface compartment. Exess water is moved to surface. If water on surface and sub surface volume is less than volx then surface water is moved to subsurface.


        public SpillingVolumeSurfaceConnection(int from, int to, string parameters) : base(from, to, parameters)
        {
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";
            string[] split = parameters.Split(new Char[] { ' ', '\t', '=', ',', '(', ')', ';' }, StringSplitOptions.RemoveEmptyEntries);
            this.volx = Convert.ToDouble(split[0], provider);

        }

        public override double calculateFlow(double[] values)
        {   double q=0;
            if (values[from] > volx)
            {
                q = (values[from] - volx) / EMPTY_TIME;
            }
            else if (values[to] > 0)
            {
                q = -values[to] / EMPTY_TIME;
                if (q > MIN_FLOW_TO_DRAIN) q = MIN_FLOW_TO_DRAIN;
            }
            flow = q;
            return q;
        }

        public override double[] getParameterArray()
        {
            throw new NotImplementedException();
        }

        public override int setParameterArray(double[] newParameters, int i)
        {
            throw new NotImplementedException();
        }

        public override void setParameters(double[] specificParameters)
        {
            throw new NotImplementedException();
        }

        public override string typeTag()
        {
            return "SpillingVolumeSurfCon";
        }

        internal override string parameterString()
        {
            return volx.ToString(CultureInfo.InvariantCulture);
        }
    }
}