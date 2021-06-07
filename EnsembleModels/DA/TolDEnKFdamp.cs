using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace EnsembleModels
{
    class TolDEnKFdamp : IDaScheme
    {
        KalmanGain KK;

        public TolDEnKFdamp()
        {

        }

        public Matrix<double> Update(Matrix<double> E, double[,] HE, double[] observations, double[,] obsCoVar, double inflationFactor, double dampning)
        {
            Matrix<double> XXX = E;
            int m_ensembleMembers = XXX.ColumnCount;
            int n_elementsinStateVector = XXX.RowCount;
            int p_DA_obs = observations.Length;
            double stdObs = Math.Sqrt(obsCoVar[0, 0]);// only implemented for a single obs 
            double tolerance = 1.5; 

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
                    hx[i] += HE[i, j];
                }
                hx[i] = hx[i] / m_ensembleMembers;
                dytemp[i] = observations[i] - hx[i];
            }
            Vector<double> dy = Vector<double>.Build.Dense(dytemp);

            double uplim = dy[0] + tolerance * stdObs;
            double downlim = dy[0] - tolerance * stdObs;

            for (int i = 0; i < p_DA_obs; i++)
            {
                for (int j = 0; j < m_ensembleMembers; j++)
                {
                    HE[i, j] -= hx[i];// substract mean to get anomalies at h
                                                             
                }
            }

            Matrix<double> HA = M.DenseOfArray(HE);

            for (int i = 0; i < p_DA_obs; i++)
            {
                for (int j = 0; j < m_ensembleMembers; j++)
                {
                   
                    if (HE[i, j] > uplim)
                    {
                        HE[i, j] -= uplim;
                    }
                    else if (HE[i, j] < downlim)
                    {
                        HE[i, j] -= downlim;
                    }
                    else
                    {
                        HE[i, j] = 0;
                    }
                }
            }
            Matrix<double> HAx = M.DenseOfArray(HE);

            Matrix<double> ObsCoVar = M.DenseOfArray(obsCoVar);
         
            KK = new KalmanGain(n_elementsinStateVector, p_DA_obs);
            KK.CaclulateGain(XXX, HA, ObsCoVar);

            //calculate dampening from dy and std HE


            double[] VarAtH = new double[p_DA_obs];
                      for (int i = 0; i < p_DA_obs; i++)
                       {
                           for (int j = 0; j < m_ensembleMembers; j++)
                           {
                               VarAtH[i] += HE[i, j]* HE[i, j];// substract mean to get anomalies at h . beeeregn var
                           }
                           VarAtH[i] = VarAtH[i] / (m_ensembleMembers-1);
                       
                       }
            double dy_std = Math.Abs(dy[0]) / Math.Sqrt(VarAtH[0]);
           
            if (dy_std > tolerance)
            {
           //        dampning = 1 / (1 + (dy_std - tolerance)*(dy_std - tolerance));// NOTE: Only implementet for single obs loc so far!
            }

         
                       XXX = XXX.Subtract( (KK.K.Multiply(HAx)).Multiply(dampning) );//XXX contains updated A
                       XXX = XXX.Multiply(inflationFactor);
                       // XXX = XXX.Subtract(KK.K.Multiply(HA));
                       //Vector<double> Obs = Vector<double>.Build.Dense(observations);


                    //   Vector<double> dx = KK.K * dy;

                       //add ensemble mean to ensemble anomelis (XXX = A + dx + x)
                       //MB: a faster way?
                       for (int i = 0; i < m_ensembleMembers; i++)
                       {
                           for (int n = 0; n < n_elementsinStateVector; n++)
                           {
                    //XXX[n, i] += (x[n] + dx[n]);
                    XXX[n, i] += x[n];
                }
                       }
                       return XXX;
                   }
               }
           }
