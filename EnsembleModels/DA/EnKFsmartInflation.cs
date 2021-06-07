using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace EnsembleModels
{
    public class EnKFsmartInflation : IDaScheme
    {
        KalmanGain KK;


        public EnKFsmartInflation()
        {
           
        }


        public Matrix<double> Update(Matrix<double> E, double[,] HE, double[] observations, double[,] obsCoVar, double rFactor, double dampning)
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
                D = D.TransposeThisAndMultiply( (ObsCoVar.PointwiseSqrt()).Multiply(rFactor)).Transpose();


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

               //XXX = XXX.Subtract(KK.K.Multiply(HA.Subtract(D)));//XXX contains updated A



               // XXX = XXX.Multiply(inflationFactor);
                // XXX = XXX.Subtract(KK.K.Multiply(HA));
                //Vector<double> Obs = Vector<double>.Build.Dense(observations);


                Vector<double> dx = KK.K.Multiply(dampning) * dy;

            
            for (int i = 0; i < m_ensembleMembers; i++)
            {
                for (int n = 0; n < n_elementsinStateVector; n++)
                {
                    XXX[n, i] = XXX[n, i] * (x[n] + dx[n]) / x[n];//XXX contains updated A (scaled by change (unchanged std/mean)
                }
            }



            //add ensemble mean to ensemble anomelis (XXX = A + dx + x)
            //MB: a faster way?

            //Calculate var and K at observed locations h
           // double[] VarAtH = new double[p_DA_obs];
            //double[] KAtH = new double[p_DA_obs];
 /*           for (int i = 0; i < p_DA_obs; i++)
            {
                for (int j = 0; j < m_ensembleMembers; j++)
                {
                    VarAtH[i] += HE[i, j]* HE[i, j];// substract mean to get anomalies at h . beeeregn var
                }
                VarAtH[i] = VarAtH[i] / (m_ensembleMembers-1);
                //KAtH[i] = VarAtH[i] / (VarAtH[i] + obsCoVar[i,i]);
            }
*/

//sektion for ekstra inflationsstoej. 
          //  double[] dd2 = new double[HA.RowCount * HA.ColumnCount];
           // MathNet.Numerics.Distributions.Normal.Samples(dd2, 0, 1.0);
          //  Matrix<double> D2 = (M.Dense(HA.RowCount, HA.ColumnCount, dd));
            //D2 = D2.TransposeThisAndMultiply(ObsCoVar.PointwiseSqrt()).Transpose();lkj
            //XXX = XXX.Subtract(KK.K.Multiply(D2));//hvor D2 er støj der fordeles med K som en slags inflation der IKKE påvirker K.


            for (int n = 0; n < n_elementsinStateVector; n++)
            {
                //double tempInflate = (1 + (inflationFactor - 1) * (Math.Abs((KK.K[n, 0] / (KAtH[0]+1)) * dy[0] / (Math.Sqrt(VarAtH[0])+Math.Sqrt(obsCoVar[0, 0])))));//mangler af justify addition of obs std (perhabs som sum of variances) til sidst og  Kalman konstanten 1
               // tempInflate = 1;
                for (int i = 0; i < m_ensembleMembers; i++)
                {

                    //inflate first
                    //double tempInflate = (1 + (inflationFactor - 1) * (KK.K[n, 0] / KAtH[0]) * (dy[0] / Math.Sqrt(VarAtH[0])));
                 //   XXX[n, i] = XXX[n, i] * tempInflate; // virker kun med en obs .Skift 100 ud med custom factor.
                    //then recontruct ensamble



                    XXX[n, i] += (x[n] + dx[n]);
                    }
                }
            return XXX;
            }

       
    }
}
