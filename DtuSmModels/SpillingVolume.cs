using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtuSmModels
{
    internal class SpillingVolume : Connection
    {
        double volx;//max volume in the sub surface compartment before spilling occurs. Correspond to critical level in MU.
        int nTimeStepsWithSpilling; //number of timesteps with volume above volx

        public SpillingVolume(int from, int to, string parameters) : base(from, to, parameters)
        {
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";
            string[] split = parameters.Split(new Char[] { ' ', '\t', '=', ',', '(', ')', ';' }, StringSplitOptions.RemoveEmptyEntries);
            this.volx = Convert.ToDouble(split[0], provider);

        }

        public override double calculateFlow(double[] values)
        {
            if (values[from] > volx) nTimeStepsWithSpilling++;
            return 0;
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
            return "SpillingVolume";
        }

        internal override string parameterString()
        {
            return volx.ToString(CultureInfo.InvariantCulture);
        }
    }
}