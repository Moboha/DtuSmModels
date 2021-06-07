using System;
using System.Collections.Generic;
using System.Globalization;

namespace DtuSmModels
{
    internal class PwlGradientBasedFlow : Connection
    {
        internal DerivedValue WlUp; //upstream water level
        internal DerivedValue WlDown; //downstream water level
        internal DerivedValue flowFactor; //relates the squared wl difference with the flow (q = flowFactor * sqrt(WlUp-WlDown))

        public PwlGradientBasedFlow(int from, int to, string parameters) : base(from, to, parameters)
        {
            string[] split = parameters.Split(new Char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

            WlUp = new DerivedValue(split[0]);
            WlDown = new DerivedValue(split[1]);
            flowFactor = new DerivedValue(split[2]);

        }

        public override double calculateFlow(double[] values)
        {
            double wl1 = WlUp.calculate(values[from]);
            double wl2 = WlDown.calculate(values[to]);
            
            if (wl1 > wl2)
            {
                flow = flowFactor.calculate(wl1) * Math.Sqrt(wl1 - wl2);
            }
            else
            {
                flow = - flowFactor.calculate(wl2) * Math.Sqrt(wl2 - wl1);
            }

            return flow;

        }

        public override double[] getParameterArray()
        {
            double[] xparams = new double[3 + 2 * (flowFactor.Np + WlUp.Np + WlDown.Np )];//The 3 is for the number of points in each DerivedValue set.
            
            xparams[0] = flowFactor.Np;
            xparams[1] = WlUp.Np;
            xparams[2] = WlDown.Np;
            int i = 2;
            for (int j = 0; j < flowFactor.Np; j++)
            {   i++; xparams[i] = flowFactor.xStarts[j];
                i++; xparams[i] = flowFactor.qs[j];
            };
            for (int j = 0; j < WlUp.Np; j++)
            {
                i++; xparams[i] = WlUp.xStarts[j];
                i++; xparams[i] = WlUp.qs[j];
            };
            for (int j = 0; j < WlDown.Np; j++)
            {
                i++; xparams[i] = WlDown.xStarts[j];
                i++; xparams[i] = WlDown.qs[j];
            };
            return xparams;
          //  throw new System.NotImplementedException();
        }

        public override int setParameterArray(double[] newParameters, int i)
        {
            throw new System.NotImplementedException();
        }

        public override void setParameters(double[] xparams)
        {
            flowFactor.Np = (int) xparams[0];
            WlUp.Np = (int) xparams[1];
            WlDown.Np = (int) xparams[2];

            int i = 2;
            for (int j = 0; j < flowFactor.Np; j++)
            {
                i++; flowFactor.xStarts[j] = xparams[i];
                i++;  flowFactor.qs[j] = xparams[i];
            };
            flowFactor.calculateSlopesAndIntersects();
            for (int j = 0; j < WlUp.Np; j++)
            {
                i++; WlUp.xStarts[j] = xparams[i];
                i++; WlUp.qs[j] = xparams[i];
            };
            WlUp.calculateSlopesAndIntersects();
            for (int j = 0; j < WlDown.Np; j++)
            {
                i++; WlDown.xStarts[j] = xparams[i];
                i++; WlDown.qs[j] = xparams[i];
            };
            WlDown.calculateSlopesAndIntersects();
            //throw new System.NotImplementedException();
        }

        public override string typeTag()
        {
            return "PwlGradientBasedFlow";
            
        }

        internal override string parameterString()
        {
            throw new System.NotImplementedException();
        }
    }
}