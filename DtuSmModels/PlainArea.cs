using System;
using System.Globalization;

namespace DtuSmModels
{
   internal class PlainArea : Catchment
    {


        private double depthToM3PrS;
        //private int index;
        

        public PlainArea(Node node, string parameters)
        {
            try
            {
            System.Globalization.NumberFormatInfo provider = new System.Globalization.NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";
            string[] split = parameters.Split(new Char[] { ' ', '\t', '=', ',', '(', ')', ';' }, StringSplitOptions.RemoveEmptyEntries);
            double imperviousAreaInM2 = Convert.ToDouble(split[0], provider);
            this.node = node;
            depthToM3PrS = imperviousAreaInM2 / 1000 /  60;
            }
            catch (Exception ex)
            {
               
                Exception ex2 = new Exception("PlainArea constructor error: " + node.name + " " + parameters , ex);  
    throw ex2;
                       
            }
     
        }

        public override double getNextFlowInM3PrS()
        {
            try
            {
            double flow = rain.getRain(index) * depthToM3PrS;
            index++;
            return flow;
            }
            catch (Exception e)
            {

                if (rain.data == null)
                {
                    throw new Exception("Catchment to node: " + node.name + " has no rainfall data assigned");
                }
                else
                {
                throw new Exception("Following exception was thrown when trying to calculate the runoff from catchment " + node.name + " for index " + index + " in the rainfall data : " + e.Message);
                }

            }


        }

//        internal void setRainfallData(RainfallData raindata)
//        {
//            this.rain = raindata;
//            index = 0;
//        }

        public override string typeTag()
        {
            return "PlainArea";
        }

        internal override string parameterString()
        {
            
            return ("(" + (this.depthToM3PrS * 1000 *  60).ToString(CultureInfo.InvariantCulture) + ")");
        }

        public override double[] getParameterArray()
        {
            double[] area = new double[1];
            area[0] = this.depthToM3PrS * 1000 *  60;
            

            return area;
        }

        public override void setParameters(double[] area)
        {

          
            this.depthToM3PrS = area[0] / 1000 /  60;

        }

        public override int setParameterArray(double[] newParameters, int i)
        {
            int numberOfParameters = 1;
            double[] xx = new double[numberOfParameters];
            Array.Copy(newParameters, i, xx, 0, numberOfParameters);
            setParameters(xx);
            return numberOfParameters;
        }
    }
}