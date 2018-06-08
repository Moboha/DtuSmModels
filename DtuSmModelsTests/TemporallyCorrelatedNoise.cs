using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtuSmModelsTests
{
    class TemporallyCorrelatedNoise
    {

            //The Ensemble Kalman Filter: theoretical formulation and practical implementation by evensen for details of inspiration. 

            private double[] varianceScaling;
            private double[] rho;
            private double[] sqx;
            private double[] value;
            private int n;//number of elements in arrays.
            private Random rand;
            private double[] truncateLimit;
            private bool bTruncate = false;
            private bool[] _bStoch;

            /*
             * deCorrelationTimeTau is in seconds, dt is delte t in seconds
             */
            public TemporallyCorrelatedNoise(double[] variance, double[] decorrelationTimeTau, double dt, double truncateLimitInStds)
            {

                n = variance.Length;
                value = new double[n];
                rho = new double[n];
                sqx = new double[n];
                _bStoch = new bool[n];
                varianceScaling = new double[n];
                rand = new Random();
                if (truncateLimitInStds != 0)
                {
                    bTruncate = true;
                    truncateLimit = new double[n];
                }


                for (int i = 0; i < n; i++)
                {

                    if (variance[i] > 0)
                    {
                        _bStoch[i] = true;
                        varianceScaling[i] = Math.Sqrt(12 * variance[i]);

                        if (decorrelationTimeTau[i] <= dt)
                        {
                            rho[i] = 0;
                        }
                        else
                        {
                            rho[i] = 1.0 - (dt / decorrelationTimeTau[i]);
                        }

                        sqx[i] = Math.Sqrt(1 - rho[i] * rho[i]);
                        value[i] = (rand.NextDouble() - 0.5) * varianceScaling[i];
                        if (bTruncate) truncateLimit[i] = truncateLimitInStds * Math.Sqrt(variance[i]);
                    }
                    else
                    {
                        _bStoch[i] = false;
                    }
                }

            }


            public double[] drawNext()
            {

                for (int i = 0; i < n; i++)
                {

                    if (_bStoch[i])
                    {
                        double w = (rand.NextDouble() - 0.5) * varianceScaling[i];
                        value[i] = value[i] * rho[i] + sqx[i] * w;

                        if (bTruncate)
                        {
                            if (value[i] > truncateLimit[i]) value[i] = truncateLimit[i];
                            else if (value[i] < -truncateLimit[i]) value[i] = -truncateLimit[i];
                        }
                    }
                }

                //  Console.WriteLine(value[0] + " " + value[1] + " " + value[2] + " " + value[3]);
                //   double[] asdf = new double[4];
                //   Array.Copy(value, asdf, 4);
                //   return asdf;

                // return new double[] { 0, 0, 0, 0 };
                return value;
            }



            public void doTruncate(double timesStd)
            {
                bTruncate = true;

            }

      
    }

}

