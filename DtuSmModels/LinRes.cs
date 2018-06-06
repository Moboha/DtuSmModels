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
            throw new NotImplementedException();
        }


        public override int setParameterArray(double[] newParameters, int i)
        {
            throw new NotImplementedException("asdfasdfsaaddd d");
        }

        public override void setParameters(double[] specificParameters)
        {
            throw new NotImplementedException();
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