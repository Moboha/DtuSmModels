using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace EnsembleModels
{
    public interface IDaScheme
    {
        Matrix<double> Update(Matrix<double> E, double[,] HE, double[] observations, double[,] obsCoVar, double inflationFactor, double dampeningFactor);

    }
}
