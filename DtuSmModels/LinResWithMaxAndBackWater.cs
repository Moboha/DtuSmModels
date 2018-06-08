using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtuSmModels
{
    class LinResWithMaxAndBackWater : Connection
    {
        public LinResWithMaxAndBackWater(int from, int to, string parameters) : base(from, to, parameters)
        {
            throw new NotImplementedException();
        }

        public override double calculateFlow(double[] values)
        {
            throw new NotImplementedException();
        }

        public override double[] getParameterArray()
        {
            throw new NotImplementedException();
        }

        public override int setParameterArray(double[] newParameters, int i)
        {
            throw new NotImplementedException("asdfasdf");
        }

        public override void setParameters(double[] specificParameters)
        {
            throw new NotImplementedException();
        }

        public override string typeTag()
        {
            throw new NotImplementedException();
        }

        internal override string parameterString()
        {
            throw new NotImplementedException();
        }
    }
}
