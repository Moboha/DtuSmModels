using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtuSmModels
{
    public class DerivedValue
    {   //Class for calculating derived values from the state vector, that are required for the dynamic calculations.
        //todo: Most of this class is a replica of code from PieceWiseLinRes. Consider rewriting. 
        //

        internal double[] xStarts;//start of intervals. 
        internal double[] slopes;
        internal double[] intersects;
        internal int Np;//number of points that defines the intervals;
        internal int iInterval;

        internal double[] qs; //kan spare 25 procent af memory ved at omskrive til uden q


        public DerivedValue(string parameters) 
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
                slopes[i] = (qs[i + 1] - qs[i]) / (xStarts[i + 1] - xStarts[i]);
                intersects[i] = qs[i] - slopes[i] * xStarts[i];
            }
            slopes[Np - 1] = Double.NaN;
        }

        internal void calculateSlopesAndIntersects()
        {
            for (int i = 0; i < Np - 1; i++)
            {
                slopes[i] = (qs[i + 1] - qs[i]) / (xStarts[i + 1] - xStarts[i]);
                intersects[i] = qs[i] - slopes[i] * xStarts[i];
            }
            slopes[Np - 1] = Double.NaN;
        }

        public double calculate(double vol)
        {
            if (vol < xStarts[iInterval])
            {
                iInterval--;
                while (vol < xStarts[iInterval]) { iInterval--; };
            }
            else //if larger than (or equals)
            {
                while (vol > xStarts[iInterval + 1]) { iInterval++; };
            }

            double value = intersects[iInterval] + vol * slopes[iInterval];
            return value;
        }

    }
}
