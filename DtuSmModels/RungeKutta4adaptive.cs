using System;

namespace DtuSmModels
{
    public class RungeKutta4adaptive : Solver
    {
        double[] k1;
        double[] k2;
        double[] k3;
        double[] k4;
        static double oneSixth = 1.0 / 6.0;
        int L;


        public RungeKutta4adaptive(MainModel model) : base(model)
        {
            L = model.lengthOfStateVector;
            k1 = new double[L];
            k2 = new double[L];
            k3 = new double[L];
            k4 = new double[L];


        }
        public override double[] solve(double dt, double[] volumes, double[] forcing)
        {
            double[] newVolumes = new double[1];//just to make VS accept 
            bool bSuccess = false;
            int tad = 1; //time adaption denominator

            while (!bSuccess)
            {

                try
                {
                    for (int i = 1; i <= tad; i++)
                    {
                        if (i == 1)
                        {
                            newVolumes = solvex(dt / tad, volumes, forcing);
                        }
                        else
                        {
                            newVolumes = solvex(dt / tad, newVolumes, forcing);
                        }
                    }
                    bSuccess = true;
                }
                catch (Exception e)
                {
                    bSuccess = false;
                    tad = tad * 2;
                    if (tad > 64)
                    {
                        throw new Exception("Model not stable. Adaptive time step has rearch its minimum size of <1s. Causing exeption: " + e.Message);
                    }
                }
            }


            return newVolumes;

        }
        private double[] solvex(double dt, double[] volumes, double[] forcing)
        {
            Array.Clear(k1, 0, L);
            Array.Clear(k2, 0, L);
            Array.Clear(k3, 0, L);
            Array.Clear(k4, 0, L);

            k1 = arrMult(model.calculateDyDt(volumes, forcing), dt);
            k2 = arrMult(model.calculateDyDt(arrSum(volumes, arrMult(k1, 0.5)), forcing), dt);
            k3 = arrMult(model.calculateDyDt(arrSum(volumes, arrMult(k2, 0.5)), forcing), dt);
            k4 = arrMult(model.calculateDyDt(arrSum(volumes, k3), forcing), dt);

            double[] dy = arrMult(arrSum(k1, arrMult(arrSum(k2, k3), 2), k4), oneSixth);
            return arrSum(volumes, dy);
        }



        public static double[] arrMult(double[] v, double a)
        {
            var XX = new double[v.Length];
            for (int i = 0; i < v.Length; i++) XX[i] = v[i] * a;
            return XX;
        }

        public static double[] arrSum(double[] v, double[] v2)
        {
            var XX = new double[v.Length];
            for (int i = 0; i < v.Length; i++) XX[i] = v[i] + v2[i];
            return XX;
        }
        private double[] arrSum(double[] v1, double[] v2, double[] v3)
        {
            var XX = new double[v1.Length];
            for (int i = 0; i < v1.Length; i++) XX[i] = v1[i] + v2[i] + v3[i];
            return XX;
        }
    }
}