using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace DtuSmModels
{
    class LinResSurf2 : Catchment
    {
        //const double DT = 60;
            public double[] state { get; set; }//surface storage, s1 and s2, baseflow m3/s.
            double depthToVolume;//m3
            //double dt;
            double fstep; //factor to multiply on state to get next step value from initial without regarding inflow.
            double f1_2; // factor to multiply on S1 to get its next step contribution to S2
            public double meanDischargeOut { get; private set; }//in m3/s;       
 

            public LinResSurf2(Node node, string parameters)
            {

            try
            {
                System.Globalization.NumberFormatInfo provider = new System.Globalization.NumberFormatInfo();
                provider.NumberDecimalSeparator = ".";
                string[] split = parameters.Split(new Char[] { ' ', '\t', '=', ',', '(', ')', ';' }, StringSplitOptions.RemoveEmptyEntries);
                this.depthToVolume = Convert.ToDouble(split[0], provider)/1000;
                double tc = Convert.ToInt32(split[1], provider);

                this.node = node;
                
                double k = tc*60/4;// 4 due to 2 reservoirs with each tc/2 mean retention time.
                double dt = DtuSmModels.myconst.DT;

                fstep = System.Math.Exp(-dt / k);
                f1_2 = fstep * dt / k;
                state = new double[2];
                
            }
            catch (Exception ex)
            {

                Exception ex2 = new Exception("LinResSurf2 constructor error: " + node.name + " " + parameters, ex);
                throw ex2;

            }
        }

            /*
             * effective area in m2, reservoirTimeConstant in seconds, initialLoss in m, initialLossRecoveryRate in m/s, 
             */


            private void oneStepForward(double rainStepDepthInMm)
            {
                state[0] += rainStepDepthInMm* this.depthToVolume;

                if (state[0] < 0) state[0] = 0; 
            
                //lin res time update using accumulated unit responses 
                double s1 = state[0] * fstep;
                double s2 = state[1] * fstep;
                s2 += state[0] * f1_2;

                meanDischargeOut = (state[0] + state[1] - s1 - s2) / DtuSmModels.myconst.DT;

                state[0] = s1;
                state[1] = s2;

                //end time update
            }

        public override double getNextFlowInM3PrS()
        {
            try
            {
                oneStepForward(rain.getRain(index));
                index++;
                return meanDischargeOut;
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

        public override double[] getParameterArray()
        {
            
            double[] areaAndTc = new double[2];
            areaAndTc[0] = this.depthToVolume * 1000;
            areaAndTc[1] = (-DtuSmModels.myconst.DT / System.Math.Log(this.fstep)) * 4 / 60;

            return areaAndTc;
        }

        public override int setParameterArray(double[] newParameters, int i)
        {
            int numberOfParameters = 2;
            double[] xx = new double[numberOfParameters];
            Array.Copy(newParameters, i, xx, 0, 2);
            setParameters(xx);
            return numberOfParameters;
        }

        public override void setParameters(double[] areaAndTc)
        {//tc is converted to int by rounding off.
                        
            double k = Convert.ToInt32(areaAndTc[1]) * 60 / 4;;
            this.depthToVolume = areaAndTc[0] / 1000 ;
            fstep = System.Math.Exp(-DtuSmModels.myconst.DT / k);
            f1_2 = fstep * DtuSmModels.myconst.DT / k;

        }

        public override string typeTag()
        {
            return "LinResSurf2";
        }

        internal override string parameterString()
        {
            double[] areaAndTc = getParameterArray();
            return ("(" + areaAndTc[0].ToString(CultureInfo.InvariantCulture) + "," + areaAndTc[1].ToString(CultureInfo.InvariantCulture) + ")");
        }
    }
}
