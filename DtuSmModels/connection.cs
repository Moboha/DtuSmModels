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
        protected double flow;
        //   static public StateVector state;

        public double getFlow()
        {
            return flow;
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
        }


        public abstract double[] getParameterArray();
        public abstract void setParameters(double[] specificParameters); //
        public abstract int setParameterArray(double[] newParameters, int i); //i is startindex in parameterarray for entire model. returns the number of parameters. 

        public abstract string typeTag();
        internal abstract string parameterString();
    }
}
