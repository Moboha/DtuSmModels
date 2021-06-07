using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtuSmModels
{   

    public abstract class Connection
    {
        //blic Compartment from;
        public int from;
        public int to;
        protected double flow;//for calculations
        //NExt four doubles should be refactored into an output object, so that is does not take up memory for connections without outputs.
        private double accFlow;// for results 
        protected double Nacc;
        private double accMassFlux;// for calculating the mean mass flux over the time period since last retrieval 
        protected double NaccMass;
        //
        public bool bIsOutput;
        public bool bFromFlowDivider;
        public bool bToFlowDivider;



        //   static public StateVector state;

        public void accumFlow(double flowx)
        {   if(Nacc == 0)
            {
                accFlow = flowx;
            }
            else
            {
                accFlow += flowx;
            }
            Nacc++;
        }

        public void accumMassFlux(double massFlux)
        {
            if (NaccMass == 0)
            {
                accMassFlux = massFlux;
            }
            else
            {
                accMassFlux += massFlux;
            }
            NaccMass++;
        }

        public double retrieveMeanFlow()
        { 
            double meanFlow;
            if(Nacc == 0)
            {
                meanFlow = accFlow;
            }
            else
            {
                meanFlow = accFlow / Nacc;
                Nacc = 0;
            }           
            return meanFlow;
        }

        public double retrieveMeanMassFlux()
        {
            double meanFlux;
            if (NaccMass == 0)
            {
                meanFlux = accMassFlux;
            }
            else
            {
                meanFlux = accMassFlux / NaccMass;
                NaccMass = 0;
            }
            return meanFlux;
        }

        abstract public double calculateFlow(double[] values);


        /*       protected Connection(Compartment from, Node to, string parameters)
               {
                   this.from = from;
                   this.to = to;
               }*/

        protected Connection(int from, int to, string parameters)
        {
            this.from = from;
            this.to = to;
            bIsOutput = false;
            bFromFlowDivider = false;
            bToFlowDivider = false;
        }


        public abstract double[] getParameterArray();
        public abstract void setParameters(double[] specificParameters); //
        public abstract int setParameterArray(double[] newParameters, int i); //i is startindex in parameterarray for entire model. returns the number of parameters. 

        public abstract string typeTag();
        internal abstract string parameterString();
    }
}
