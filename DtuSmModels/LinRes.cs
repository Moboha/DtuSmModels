using System;
using System.Globalization;

namespace DtuSmModels
{
    internal class LinRes : Connection
    {
        public static readonly string tag = "LinRes";
        protected double timeConstant;


        public LinRes(int from, int to, string parameters) : base(from, to, parameters)
        {
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";
            this.timeConstant = Convert.ToDouble(parameters, provider);
        }

        public override double calculateFlow(double[] values)
        {
            flow = values[from] * timeConstant;
            return flow;
            //return state.values[from]* timeConstant;           
        }

        public override double[] getParameterArray()
        {


            double[] xparams = new double[1];
            xparams[0] = this.timeConstant;
            return xparams;
            
        }


        public override int setParameterArray(double[] newParameters, int i)
        {
            int N = 1;
            double[] xx = new double[N];
            Array.Copy(newParameters, i, xx, 0, N);
            setParameters(xx);
            return N;
        }

        public override void setParameters(double[] specificParameters)
        {
            this.timeConstant = specificParameters[0];
            if (this.timeConstant < 0.00000000000001) this.timeConstant = 0.00000000000001;//simply to avid non-positive values
        }

        public override string typeTag()
        {
            return tag;
        }

        internal override string parameterString()
        {
            return timeConstant.ToString(CultureInfo.InvariantCulture);
        }
    }
}