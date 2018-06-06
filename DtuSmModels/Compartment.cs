using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtuSmModels
{
    public abstract class Compartment : Node
    {

        //public double volume { get;}
       // public int index { get; }
        private static int numberOfCompartments;

        protected Compartment(string name) : base(name)
        {
            this.index = numberOfCompartments;
            numberOfCompartments++;

        }

        public int totalNumberOfCompartments()
        {
            return numberOfCompartments;
        }


        static public void resetNumberOfCompartments()
        {
            numberOfCompartments = 0;
        }
    }
}
