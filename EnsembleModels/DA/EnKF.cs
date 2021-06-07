using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace EnsembleModels
{
    public class EnKF : IDaScheme
    {
        KalmanGain KK;


        public EnKF()
        {
           
        }


        public Matrix<double> Update(Matrix<double> E, double[,] HE, double[] observations, double[,] obsCoVar, double inflationFactor, double dampning)
        {
            Matrix<double> XXX = E;
            int m_ensembleMembers = XXX.ColumnCount;
            int n_elementsinStateVector = XXX.RowCount;
            int p_DA_obs = observations.Length;


            var M = Matrix<double>.Build;
            Vector<double> x = XXX.RowSums().Divide(m_ensembleMembers); //ensemble mean

                //substract ensemble mean to get ensemble anomelis (XXX = A)
                //MB: a faster way?
                for (int i = 0; i < m_ensembleMembers; i++)
                {
                    for (int n = 0; n < n_elementsinStateVector; n++)
                    {
                        XXX[n, i] -= x[n];
                    }
                }


                //get HA
                //double[,] HE = new double[p_DA_obs, m_ensembleMembers];
                double[] hx = new double[p_DA_obs];//mean of y
                double[] dytemp = new double[p_DA_obs];//innovation, dy = y - Hx(p x 1)

                for (int i = 0; i < p_DA_obs; i++)
                {  
                    for (int j = 0; j < m_ensembleMembers; j++)
                    {
                    
                        hx[i] += HE[i,j];
                    }
                    hx[i] = hx[i] / m_ensembleMembers;
                    dytemp[i] = observations[i] - hx[i];
                }
                Vector<double> dy = Vector<double>.Build.Dense(dytemp);


                for (int i = 0; i < p_DA_obs; i++)
                {
                    for (int j = 0; j < m_ensembleMembers; j++)
                    {
                        HE[i, j] -= hx[i];// substract mean to get anomalies at h
                    }
                }

                Matrix<double> HA = M.DenseOfArray(HE);


                double[] dd = new double[HA.RowCount * HA.ColumnCount];
                MathNet.Numerics.Distributions.Normal.Samples(dd, 0, 1.0);
                Matrix<double> D = (M.Dense(HA.RowCount, HA.ColumnCount, dd));
                Matrix<double> ObsCoVar = M.DenseOfArray(obsCoVar);
                D = D.TransposeThisAndMultiply(ObsCoVar.PointwiseSqrt()).Transpose();


                // from SAKOV's assimilate.m
                // D = randn(p, m) * sqrt(r * rfactor);
                //  % Subtract the ensemble mean from D to ensure that update of the
                //  % anomalies does not perturb the ensemble mean. This reduces the
                //  % variance of each sample by a factor of 1 - 1 / m(I think).
                //  %
                //   d = mean(D')';
                //   D = sqrt(m / (m - 1)) * (D - repmat(d, 1, m));

                //   A = A + K * (D - HA);
                KK = new KalmanGain(n_elementsinStateVector, p_DA_obs);
                KK.CaclulateGain(XXX, HA, ObsCoVar);
                XXX = XXX.Subtract(KK.K.Multiply(HA.Subtract(D)).Multiply(dampning));//XXX contains updated A
                XXX = XXX.Multiply(inflationFactor);
                // XXX = XXX.Subtract(KK.K.Multiply(HA));
                //Vector<double> Obs = Vector<double>.Build.Dense(observations);


                Vector<double> dx = KK.K.Multiply(dampning) * dy;

                //add ensemble mean to ensemble anomelis (XXX = A + dx + x)
                //MB: a faster way?
                for (int i = 0; i < m_ensembleMembers; i++)
                {
                    for (int n = 0; n < n_elementsinStateVector; n++)
                    {
                        XXX[n, i] += (x[n] + dx[n]);
                    }
                }
            return XXX;
            }

       
    }
}
