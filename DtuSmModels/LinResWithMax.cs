using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace DtuSmModels
{
    internal class LinResWithMax : Connection
    {
        private double QMAX;
        protected double timeConstant;

        public LinResWithMax(int from,int to, string parameters) : base(from, to, parameters)
        {

          
                NumberFormatInfo provider = new NumberFormatInfo();
                provider.NumberDecimalSeparator = ".";
            string[] split = parameters.Split(new Char[] { ' ', '\t', '=' , '(', ')' , ';', ','}, StringSplitOptions.RemoveEmptyEntries);
            this.timeConstant = Convert.ToDouble(split[0], provider);
            QMAX = Convert.ToDouble(split[1], provider);

        }


        public override double calculateFlow(double[] values)
        {
            //double q = state.values[from] * timeConstant;
            double q = values[from] * timeConstant;

            if (q > QMAX) q = QMAX;
            flow = q;
            return q;

        }

        public override double[] getParameterArray()
        {
            var xparams = new double[2];
            xparams[0] = this.timeConstant;
            xparams[1] = this.QMAX;
            return xparams;

        }

        public override int setParameterArray(double[] xparams, int i)
        {
            timeConstant = xparams[i + 0];
            QMAX = xparams[i + 1];
            return 2;
        }

        public override string typeTag()
        {
            return "LinResWithMax";
        }

        internal override string parameterString()
        {
            return ("(" + timeConstant.ToString(CultureInfo.InvariantCulture) + ";" + QMAX.ToString(CultureInfo.InvariantCulture) +")");
        }

        public override void setParameters(double[] specificParameters)
        {
            throw new NotImplementedException();
        }
    }
}
