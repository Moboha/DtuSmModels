using System;
using System.Globalization;

namespace DtuSmModels
{
   internal class TA1 : Catchment
    {
        
        //private double imperviousAreaInM2; 
        
        private int tc;//in minutes
        public double depthToM3PrS;
        private double sumvar = 0;
       // private int index;
        //private RainfallData rain;
        

        public TA1(Node node, string parameters)
        {
            try
            {
            System.Globalization.NumberFormatInfo provider = new System.Globalization.NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";
            string[] split = parameters.Split(new Char[] { ' ', '\t', '=', ',', '(', ')', ';' }, StringSplitOptions.RemoveEmptyEntries);
            double imperviousAreaInM2 = Convert.ToDouble(split[0], provider);
            tc = Convert.ToInt32(split[1], provider);
            this.node = node;
            depthToM3PrS = imperviousAreaInM2 / 1000 / (tc * 60);
            }
            catch (Exception ex)
            {
               
                Exception ex2 = new Exception("TA1 constructor error: " + node.name + " " + parameters , ex);  
    throw ex2;
              
            
            }




       
        }

        public override double getNextFlowInM3PrS()
        {
            try
            {
            sumvar += rain.getRain(index) - rain.getRain(index - tc);
            index++;
                if (sumvar < 0) sumvar = 0; 
            return sumvar * depthToM3PrS;
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

 //       internal void setRainfallData(RainfallData raindata)
 //       {
 //           this.rain = raindata;
 //           index = 0;
 //       }

        public override string typeTag()
        {
            return "TA1";
        }

        internal override string parameterString()
        {
            
            return ("(" + (this.depthToM3PrS * 1000 * (tc * 60)).ToString(CultureInfo.InvariantCulture) + "," + tc.ToString(CultureInfo.InvariantCulture) + ")");
        }

        public override double[] getParameterArray()
        {//depthToM3PrS = imperviousAreaInM2 / 1000 / (tc * 60);
            //note that tc is INT but here returned as double. 
            double[] areaAndTc = new double[2];
            areaAndTc[0] = this.depthToM3PrS * 1000 * (tc * 60);
            areaAndTc[1] = this.tc;

            return areaAndTc;
        }

        public override void setParameters(double[] areaAndTc)
        {//tc is converted to int by rounding off.

            this.tc = Convert.ToInt32(areaAndTc[1]);
            this.depthToM3PrS = areaAndTc[0] / 1000 / (tc * 60);

            if(this.tc<1)  this.tc = 1;
            if (this.depthToM3PrS < 0.000000000001) this.depthToM3PrS = 0.000000000001;//simply to avid non-positive values

        }

        public override int setParameterArray(double[] newParameters, int i)
        {
            int numberOfParameters = 2;
            double[] xx = new double[numberOfParameters];
            Array.Copy(newParameters, i, xx, 0, 2);
            setParameters(xx);
            return numberOfParameters;
        }
    }
}