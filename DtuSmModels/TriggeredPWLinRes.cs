using System;
using System.Collections.Generic;
using System.Globalization;

namespace DtuSmModels
{
    internal class TriggeredPWLinRes : PieceWiseLinRes
    {

        private double startVolume;
        private double stopVolume;
        private bool isOn;

        public TriggeredPWLinRes(int from, int to, string parameters) : base(from, to, extractPwParams(parameters))
        {
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";

            string[] split = parameters.Split(new Char[] { ',', '(', ';' }, 3, StringSplitOptions.RemoveEmptyEntries);
            this.startVolume = Convert.ToDouble(split[0], provider);
            this.stopVolume = Convert.ToDouble(split[1], provider);
            this.isOn = false;

        }

        private static string extractPwParams(string parameters)
        {
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";

            string[] split = parameters.Split(new Char[] { ',', '(', ';' }, 3, StringSplitOptions.RemoveEmptyEntries);
            return "(" + split[2];
        }


        public override double calculateFlow(double[] values)
        {
            if (values[from] > startVolume)
            {
                isOn = true;
            }
            else if (values[from] < stopVolume)
            {
                isOn = false;
            }


            if (isOn)
            {
                return base.calculateFlow(values);
            }
            else
            {
                return 0;
            }

        }

        public override double[] getParameterArray()
        {   double[] PWLparam = base.getParameterArray();
            double[] param = new double[PWLparam.Length + 2];
            PWLparam.CopyTo(param, 2);
            param[0] = this.startVolume;
            param[1] = this.stopVolume;
            return param;
        }


        internal override string parameterString()
        {
            var us = CultureInfo.InvariantCulture;

            List<string> points = new List<string>();
            points.Add("(");
            points.Add(startVolume.ToString(us) + "," + stopVolume.ToString(us) + ";");
            int i = 0;
            for (; i < Np - 1; i++)
            {
                points.Add(xStarts[i].ToString(us) + "," + (intersects[i] + slopes[i] * xStarts[i]).ToString(us) + ";");
            }
            points.Add(xStarts[i].ToString(us) + "," + (intersects[i - 1] + slopes[i - 1] * xStarts[i]).ToString(us));

            points.Add(")");
            var result = String.Join("", points);
            return result;
        }

        public override string typeTag()
        {
            return "TriggeredPWLinRes";
        }

        public override void setParameters(double[] specificParameters)
        {
            double[] PWLparams = new double[specificParameters.Length - 2];
            Array.Copy(specificParameters, 2, PWLparams, 0, PWLparams.Length);
            this.startVolume = specificParameters[0];
            this.stopVolume = specificParameters[1];

            base.setParameters(PWLparams);
        }
        public override int setParameterArray(double[] newParameters, int i)
        {
            int N = Np * 2 + 2;
            double[] xx = new double[N];
            Array.Copy(newParameters, i, xx, 0, N);
            setParameters(xx);
            return N;
        }
    }
}
