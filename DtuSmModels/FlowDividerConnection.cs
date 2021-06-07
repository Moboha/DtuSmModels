using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtuSmModels
{
    internal class FlowDividerConnection : PieceWiseLinRes
    {
        internal FlowDivider flowDiv;

        public FlowDividerConnection(int from, int to, string parameters) : base(from, to, parameters)
        {
            base.bFromFlowDivider = true;
            

        }

        public override double calculateFlow(double[] qDividerFlows)
        {
            try
            {
            // would be more efficient to move this funtionallity to the divider object so that the flow computations for all outgoing flows can be done in one. 
            double qInd = qDividerFlows[this.from] + flowDiv.qFixed;
                //flowDiv.qFixed = 0;
            if (qInd < xStarts[iInterval])
            {
                iInterval--;
                while (qInd < xStarts[iInterval]) { iInterval--; };
            }
            else //if larger than (or equals)
            {
                while (qInd > xStarts[iInterval + 1]) { iInterval++; };
            }
            flow = (intersects[iInterval] + qInd * slopes[iInterval]) * qInd;
            return flow;
            }
            catch (Exception e)
            {

                throw new Exception("Error calculating FlowDivider flow for " + this.flowDiv.name +  " catchInflow: " + flowDiv.qFixed + " DividerIndex: " + this.from + ". " + e.Message);
            }

        }      
    }
}
