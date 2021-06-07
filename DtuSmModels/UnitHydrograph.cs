using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace DtuSmModels
{
    internal class UnitHydrograph : Connection
    {
        MainModel mymain; // simply to be able to get a hold of the current time. 
        double t_current;
        double previousVolume;
        double flowOut;
        Queue<double> inflowFiFo;
        double[] unitResponse;

        public UnitHydrograph(int from, int to, string parameters, MainModel mymain) : base(from, to, parameters)
        {

            NumberFormatInfo provider = new NumberFormatInfo
            {
                NumberDecimalSeparator = "."
            };
            string[] split = parameters.Split(new Char[] { ' ', '\t', '=', ',', '(', ')', ';' }, StringSplitOptions.RemoveEmptyEntries);
            int Nunits = split.Length;
            this.inflowFiFo = new Queue<double>();
            for (int i = 0; i < Nunits; i++)
            {
                inflowFiFo.Enqueue(0);
            }
            this.unitResponse = new double[Nunits];
            this.mymain = mymain;


            for (int i = 0; i < Nunits; i++)
            {
                unitResponse[Nunits-1-i] = Convert.ToDouble(split[i], provider);

            }


        }

        public override double calculateFlow(double[] values)
        {
           // throw new NotImplementedException();
            if(t_current != mymain.t)
            {
                double inflow = (values[from] - previousVolume) / DtuSmModels.myconst.DT + this.flowOut;
                inflowFiFo.Dequeue();
                inflowFiFo.Enqueue(inflow);
                previousVolume = values[from];
                flowOut = 0;
                double[] xx = inflowFiFo.ToArray();
                for (int i = 0; i < unitResponse.Length; i++)
                {
                    flowOut += xx[i] * unitResponse[i];
                }
                t_current = mymain.t;

            }

            flow = flowOut;     
         return flowOut;

        }

        public override double[] getParameterArray()
        {
            double[] xunit = new double[unitResponse.Length];
            for (int i = 0; i < unitResponse.Length ; i++)
            {
                xunit[i] = unitResponse[unitResponse.Length - 1 - i];
            }

            return xunit;


        }

        public override int setParameterArray(double[] newParameters, int i)
        {

            throw new NotImplementedException();
        }

        public override void setParameters(double[] newUnitResponse)
        {
            int Nunits = newUnitResponse.Length;
            this.inflowFiFo = new Queue<double>();
            for (int i = 0; i < Nunits; i++)
            {
                inflowFiFo.Enqueue(0);
            }
            this.unitResponse = new double[newUnitResponse.Length];

            for (int i = 0; i < Nunits; i++)
            {
                unitResponse[Nunits - 1 - i] = newUnitResponse[i];

            }


        }

        public override string typeTag()
        {
            return "UnitHydro";
        }

        internal override string parameterString()
        {
            throw new NotImplementedException();
        }
    }
}
