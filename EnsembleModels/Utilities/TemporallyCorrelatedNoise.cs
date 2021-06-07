using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnsembleModels
{
    public class TemporallyCorrelatedNoise : IEnsembleNoise
    {

            //The Ensemble Kalman Filter: theoretical formulation and practical implementation by evensen for details of inspiration. 

            protected double varianceScaling;
            private double rho;
            private double sqx; 
            private double truncateLimit;
     
            private double[] values;
            private int n;//number of elements in arrays.
            private Random rand;
            
            private bool bTruncate = false;
            

            /*
             * deCorrelationTimeTau is in seconds, dt is delte t in seconds
             */
            public TemporallyCorrelatedNoise(int Nmembers, double variance, double decorrelationTimeTau, double dt, double truncateLimitInStds)
        {

            n = Nmembers;
            values = new double[n];
            rand = new Random();
            if (truncateLimitInStds != 0)
            {
                bTruncate = true;
            }

            InitializeParameters(variance, decorrelationTimeTau, dt, truncateLimitInStds);

            for (int i = 0; i < n; i++)
            {
                values[i] = (rand.NextDouble() - 0.5) * varianceScaling;
            }
        }

        protected void InitializeParameters(double variance, double decorrelationTimeTau, double dt, double truncateLimitInStds)
        {
                if (variance > 0)
                {                
                    varianceScaling = Math.Sqrt(12 * variance);

                    if (decorrelationTimeTau <= dt)
                    {
                        rho = 0;
                    }
                    else
                    {
                        rho = 1.0 - (dt / decorrelationTimeTau);
                    }

                    sqx = Math.Sqrt(1 - rho * rho);
                    
                    if (bTruncate) truncateLimit = truncateLimitInStds * Math.Sqrt(variance);
                }
                else
                {
                    throw new Exception("Variance must be a positive number");
                }


        }

        public double[] DrawNext()
            {

                for (int i = 0; i < n; i++)
                {
                        double w = (rand.NextDouble() - 0.5) * varianceScaling;
                        values[i] = values[i] * rho + sqx * w;

                        if (bTruncate)
                        {
                            if (values[i] > truncateLimit) values[i] = truncateLimit;
                            else if (values[i] < -truncateLimit) values[i] = -truncateLimit;
                        }
                  }

                return values;
            }



            public void DoTruncate(double timesStd)
            {
                bTruncate = true;

            }

        public double[][] DrawNext(int N)
        {
            throw new NotImplementedException();
        }

        public double[] DrawNextValuesForMember(int member, int Nsteps)
        {
                int i = member;
                double[] timeStepValues = new double[Nsteps];

                for (int j = 0; j < Nsteps; j++)
                {

                        double w = (rand.NextDouble() - 0.5) * varianceScaling;
                        values[i] = values[i] * rho + sqx * w;

                        if (bTruncate)
                        {
                            if (values[i] > truncateLimit) values[i] = truncateLimit;
                            else if (values[i] < -truncateLimit) values[i] = -truncateLimit;
                        }

 
                    timeStepValues[j] = values[i];

                }

            return timeStepValues;
  

        }
    }

}

