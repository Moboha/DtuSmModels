using System;
using System.Collections.Generic;
using System.Globalization;

namespace DtuSmModels
{
    internal class PieceWiseLinRes : Connection
    {
        internal double[] xStarts;//start of intervals. 
        internal double[] slopes;
        internal double[] intersects;
        internal int Np;//number of points that defines the intervals;
        internal int iInterval;

        double[] qs; //kan spare 25 procent af memory ved at omskrive til uden q
        

        public PieceWiseLinRes(int from, int to, string parameters) : base(from, to, parameters)
        {
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";
            string[] split = parameters.Split(new Char[] { ' ', '\t', '=', ',', '(', ')', ';' }, StringSplitOptions.RemoveEmptyEntries);
            this.Np = (split.Length / 2);
            this.xStarts = new double[Np];
            this.slopes = new double[Np];
            this.intersects = new double[Np];

            // double[] qs = new double[Nint]; //er ikke nødvendigt at gemme q.
            qs = new double[Np];
            int j = 0;
            for (int i = 0; i < split.Length; i++)
            {
                xStarts[j] = Convert.ToDouble(split[i], provider);
                i++;
                qs[j] = Convert.ToDouble(split[i], provider);
                j++;
            }

            calculateSlopesAndIntersects(qs); //to ensure an exeption is thrown if vol gets outside defined model space.

        }

        private void calculateSlopesAndIntersects(double[] qs)
        {
            for (int i = 0; i < Np - 1; i++)
            {

//                if ((qs[i+1] / xStarts[i+1]) > 0.06)
//                {                       
//                    throw new Exception("flow to volume ratio is not allowed to superseed 0.06s-1. In connection between node nr. " + this.from.ToString() + "  and " + this.to.ToString() + "the problematic volume and flow point is: "  + xStarts[i+1].ToString() + " " + qs[i+1].ToString());
//                }

                slopes[i] = (qs[i + 1] - qs[i]) / (xStarts[i + 1] - xStarts[i]);
                intersects[i] = qs[i] - slopes[i] * xStarts[i];

            }
            slopes[Np - 1] = Double.NaN;
        }

        public override double calculateFlow(double[] values)
        {

            //double vol = state.values[from];
            double vol = values[from];

            if (vol<xStarts[iInterval])
            {
                iInterval--;
                while (vol < xStarts[iInterval]) { iInterval--; };
            }
            else //if larger than (or equals)
            {   
                while (vol > xStarts[iInterval+1]) { iInterval++; };
 
            }
            flow = intersects[iInterval] + vol * slopes[iInterval];
            return flow;
           
        }

        public override double[] getParameterArray()
        {


            double[] xparams = new double[2 * intersects.Length ];
            int j = 0;
            int i = 0;
            for (; i < Np - 1; i++)
            {
                xparams[j] = xStarts[i];
                j++;
                xparams[j] = (intersects[i] + slopes[i] * xStarts[i]);
                j++;
            }
            xparams[j] = xStarts[i];
            xparams[j+1] = (intersects[i - 1] + slopes[i - 1] * xStarts[i]);
            return xparams;
        }

        public override int setParameterArray(double[] newParameters, int i) //Requires the number of parameters is allready known, so this method only works when the model has been initialized and the number of parameters is not changed. 
        {  
            int N = Np * 2;
            double[] xx = new double[N];
            Array.Copy(newParameters, i, xx, 0, N);
            setParameters(xx);
            return N;
        }

        public override string typeTag()
        {
            return "PieceWiseLinRes";
        }

        internal override string parameterString()
        {
            var us = CultureInfo.InvariantCulture;

            List<string> points = new List<string>();
            points.Add("(");
            int i = 0;
            for (; i < Np-1; i++)
            {
                points.Add(xStarts[i].ToString(us) + "," + (intersects[i] + slopes[i] * xStarts[i]).ToString(us) + ";");
            }
            points.Add(xStarts[i].ToString(us) + "," + (intersects[i-1] + slopes[i-1] * xStarts[i]).ToString(us));

            points.Add(")");
            var result = String.Join("", points);
            return result;
        }

        public override void setParameters(double[] specificParameters)
        {
            this.Np = specificParameters.Length / 2;
            xStarts = new double[Np];
            slopes = new double[Np]; ;
            intersects = new double[Np];
            this.iInterval = 0;
            qs = new double[Np]; ;

            int j = 0;
            for (int i = 0; i < specificParameters.Length; i++)
            {
                xStarts[j] = specificParameters[i];
                i++;
                qs[j] = specificParameters[i];
                j++;
            }

            calculateSlopesAndIntersects(qs); //to ensure an exeption is thrown if vol gets outside defined model space.
        }


    }
}