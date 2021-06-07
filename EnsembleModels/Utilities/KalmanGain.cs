using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace EnsembleModels
{
    public class KalmanGain
    {
        public Matrix<double> K { get; private set; }


        public KalmanGain(int n, int p)
        {
            K = MathNet.Numerics.LinearAlgebra.Double.DenseMatrix.Create(n, p, 0);
        }

        public Matrix<double> CaclulateGain(Matrix<double> Anomalies, Matrix<double> HA, Matrix<double> obsErrorCovar)
        {

            int n = Anomalies.RowCount;//number of states
            int m = Anomalies.ColumnCount;//number of ensemble members
            int p = HA.RowCount;//No of obs points
            
            K = DenseMatrix.Create(n, p, 0);

            //    HPHT = HA * HA' / (m - 1);
            //PHT = A * HA' / (m - 1);
            // R = speye(size(HA, 1)) * r;
            Matrix<double> HPHT = HA.TransposeAndMultiply(HA).Divide(m-1);
            Matrix<double> PHT = Anomalies.TransposeAndMultiply(HA).Divide(m - 1);
            Matrix<double> R = DenseMatrix.CreateIdentity(p).Multiply(obsErrorCovar);

            // K = PHT / (HPHT + R);
            K = PHT.Multiply(HPHT.Add(R).Inverse());
            return K;
             
        }

    }
}
