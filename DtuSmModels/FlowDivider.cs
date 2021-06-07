using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtuSmModels
{
    public class FlowDivider : Node
    {


        internal List<FlowDividerConnection> connections;
        internal double qFixed; //flow that is not solved for, e.g. runoff. 
        internal double qMassFixed;
        private static int numberOfDividers;
        public FlowDivider(string name) : base(name) 
        {
            this.bHasVolume = false;
            this.index = numberOfDividers;
            numberOfDividers++;
            connections = new List<FlowDividerConnection>();
        }

        public override string typeTag()
        {
            throw new NotImplementedException();
        }

        static public void resetNumberOfFlowDividers()
        {
            numberOfDividers = 0;
        }
    }
}
