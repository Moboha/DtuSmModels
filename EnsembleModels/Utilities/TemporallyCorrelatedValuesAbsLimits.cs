using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnsembleModels
{
    //Ensemble of realisations of random walks with changing mean and variance and truncated at absolute values. 
    //Intended to be used for parameter estimation. 
    public class TemporallyCorrelatedValuesAbsLimits : TemporallyCorrelatedNoise
    {
        private double upperLim;
        private double lowerLim;
        

        public TemporallyCorrelatedValuesAbsLimits(int Nmembers, double mean, double variance, double decorrelationTimeTau, double dt, double lowerLimit, double upperLimit) : base(Nmembers, 0, decorrelationTimeTau, dt, 5)
        {
            this.upperLim = upperLimit;
            this.lowerLim = lowerLimit;

         //   base(
            
        }
    }
}
