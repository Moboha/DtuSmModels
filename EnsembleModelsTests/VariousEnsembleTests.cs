using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MathNet.Numerics.LinearAlgebra;

using EnsembleModels;


namespace EnsembleModelsTests
{
    [TestClass]
    public class VariousEnsembleTests
    {
        string TestDataFolder = @"../../../EnsembleModelsTestData";



        [TestMethod]
        public void TestInitializeEnsembleFromPrmFile()
        {
            string ParameterFile = "SmallDiverseModel.prm";
            int NumberOfEnsembleMembers = 10;


            string paramsFileFullPath = TestDataFolder + "\\" + ParameterFile;
            EnsembleHandler EnsMods = new EnsembleHandler();
            EnsMods.InitializeFromFile(paramsFileFullPath, NumberOfEnsembleMembers);
            EnsembleStatistics ensstats = new EnsembleStatistics();
            ensstats.SetStats(true, false, true, true,   false);
            EnsMods.AddOutputVariable(DtuSmModels.SmOutput.OutputType.GlobalVolumen, ensstats, "");
            EnsMods.AddOutputVariable(DtuSmModels.SmOutput.OutputType.nodeVolume, ensstats, "SM3");

            double[] rainfall = new double[1200];
            rainfall[5] = 5; //5 mm of rainfall in one minute.

            IEnsembleNoise noise = new TemporallyCorrelatedNoise(NumberOfEnsembleMembers, 0.8, 60 * 10, 60, 3);
            EnsMods.AddNoiseOnRain(noise);

            EnsMods.SetRainDataForAllCatchments(rainfall);
            EnsMods.runForOneMinuteRainInput();
            EnsMods.SetRainDataForAllCatchments(rainfall);
            EnsMods.runForOneMinuteRainInput();
            var data1 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[0].data.ToArray();
            var data2 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[1].data.ToArray();
            var data3 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[2].data.ToArray();


        }

        [TestMethod]
        public void TestRunAndUpdate1()
        {
            string ParameterFile = "SmallDiverseModel.prm";
            int NumberOfEnsembleMembers = 10;


            string paramsFileFullPath = TestDataFolder + "\\" + ParameterFile;
            EnsembleHandler EnsMods = new EnsembleHandler();
            EnsMods.InitializeFromFile(paramsFileFullPath, NumberOfEnsembleMembers);
            EnsembleStatistics ensstats = new EnsembleStatistics();
            ensstats.SetStats(true, false, true, true, false);
           //EnsMods.AddOutputVariable(DtuSmModels.SmOutput.outputType.GlobalVolumen, ensstats, "");
            EnsMods.Mat_AddOutputVariable(DtuSmModels.SmOutput.OutputType.outletFlowTimeSeries, "outlet1", true, true, true, true, false);
            EnsMods.Mat_AddOutputVariableLinkFlow("SM3", "SM4", "myPipe", true, true, true, true, false);


            EnsMods.AddDA_variable(DtuSmModels.SmOutput.OutputType.outletFlowTimeSeries, "outlet1", "", "", "");


            double[] rainfall = new double[1200];
            rainfall[5] = 5; //5 mm of rainfall in one minute.

            IEnsembleNoise noise = new TemporallyCorrelatedNoise(NumberOfEnsembleMembers, 0.8, 60 * 10, 60, 3);
            EnsMods.AddNoiseOnRain(noise);
            double affald = 0.7;
            Double[] obs = { affald };
            Double[,] obsVar = { { 0.1 } };
            for (int i = 0; i < rainfall.Length/5; i++)
            {//update every fifth time step. 

                    EnsMods.SetRainDataForAllCatchments(rainfall.Skip(i*5).Take(5).ToArray());
                    EnsMods.runForOneMinuteRainInput();
                    EnsMods.UpdateToObs(obs, obsVar, 1, 1);
            }

          

            



            EnsMods.SetRainDataForAllCatchments(rainfall);
            EnsMods.runForOneMinuteRainInput();
            var data1 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[0].data.ToArray();
            var data2 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[1].data.ToArray();
            var data3 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[2].data.ToArray();


        }

        [TestMethod]
        public void TestRunAndUpdate2DApoints()
        {
            string ParameterFile = "SmallDiverseModel.prm";
            int NumberOfEnsembleMembers = 10;


            string paramsFileFullPath = TestDataFolder + "\\" + ParameterFile;
            EnsembleHandler EnsMods = new EnsembleHandler();
            EnsMods.InitializeFromFile(paramsFileFullPath, NumberOfEnsembleMembers);
            EnsembleStatistics ensstats = new EnsembleStatistics();
            ensstats.SetStats(true, false, true, true, false);
            //EnsMods.AddOutputVariable(DtuSmModels.SmOutput.outputType.GlobalVolumen, ensstats, "");
            EnsMods.Mat_AddOutputVariable(DtuSmModels.SmOutput.OutputType.outletFlowTimeSeries, "outlet1", true, true, true, true, false);
            EnsMods.Mat_AddOutputVariableLinkFlow("SM3", "SM4", "myPipe", true, true, true, true, false);


            EnsMods.AddDA_variable(DtuSmModels.SmOutput.OutputType.outletFlowTimeSeries, "outlet1", "", "", "");
            EnsMods.AddDA_variable(DtuSmModels.SmOutput.OutputType.outletFlowTimeSeries, "outlet1", "", "", "");


            double[] rainfall = new double[1200];
            rainfall[5] = 5; //5 mm of rainfall in one minute.

            IEnsembleNoise noise = new TemporallyCorrelatedNoise(NumberOfEnsembleMembers, 0.8, 60 * 10, 60, 3);
            EnsMods.AddNoiseOnRain(noise);
            
            Double[] obs = { 0.7, 0.7 };
            Double[,] obsVar = { { 0.1, 0 }, {0, 0.1} };
            for (int i = 0; i < rainfall.Length / 5; i++)
            {//update every fifth time step. 

                EnsMods.SetRainDataForAllCatchments(rainfall.Skip(i * 5).Take(5).ToArray());
                EnsMods.runForOneMinuteRainInput();
                EnsMods.UpdateToObs(obs, obsVar, 1, 1);
            }







            EnsMods.SetRainDataForAllCatchments(rainfall);
            EnsMods.runForOneMinuteRainInput();
            var data1 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[0].data.ToArray();
            var data2 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[1].data.ToArray();
            var data3 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[2].data.ToArray();


        }

        [TestMethod]
        public void TestRunAndUpdate_InitializeFromFile()
        {
            string ParameterFile = "SmallDiverseModelEnsemble.prm";
            int NumberOfEnsembleMembers = 10;

            string paramsFileFullPath = TestDataFolder + "\\" + ParameterFile;
            EnsembleHandler EnsMods = new EnsembleHandler();
            EnsMods.InitializeFromFile(paramsFileFullPath, NumberOfEnsembleMembers);
            //EnsMods.AddOutputVariable(DtuSmModels.SmOutput.outputType.GlobalVolumen, ensstats, "");
            //EnsMods.Mat_AddOutputVariable(DtuSmModels.SmOutput.OutputType.outletFlowTimeSeries, "outlet1", true, true, true, true, false);
            //EnsMods.Mat_AddOutputVariableLinkFlow("SM3", "SM4", "myPipe", true, true, true, true, false);

            //EnsMods.AddDA_variable(DtuSmModels.SmOutput.OutputType.outletFlowTimeSeries, "outlet1", "", "", "");


            double[] rainfall = new double[1200];
            rainfall[5] = 5; //5 mm of rainfall in one minute.

            IEnsembleNoise noise = new TemporallyCorrelatedNoise(NumberOfEnsembleMembers, 0.8, 60 * 10, 60, 3);
            EnsMods.AddNoiseOnRain(noise);
            double affald = 0.7;
            Double[] obs = { affald };
            Double[,] obsVar = { { 0.1 } };
            for (int i = 0; i < rainfall.Length / 5; i++)
            {//update every fifth time step. 

                EnsMods.SetRainDataForAllCatchments(rainfall.Skip(i * 5).Take(5).ToArray());
                EnsMods.runForOneMinuteRainInput();
                EnsMods.UpdateToObs(obs, obsVar, 1, 1);
            }



            EnsMods.SetRainDataForAllCatchments(rainfall);
            EnsMods.runForOneMinuteRainInput();
            var data1 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[0].data.ToArray();
            var data2 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[1].data.ToArray();
            var data3 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[2].data.ToArray();


        }

        [TestMethod]
        public void TestRunAndUpdate_InitializeFromFile2()
        {
            string ParameterFile = "SlowAndFast_WL_output_ensemble.prm";
            int NumberOfEnsembleMembers = 10;

            string paramsFileFullPath = TestDataFolder + "\\" + ParameterFile;
            EnsembleHandler EnsMods = new EnsembleHandler();
            EnsMods.InitializeFromFile(paramsFileFullPath, NumberOfEnsembleMembers);
            //EnsMods.AddOutputVariable(DtuSmModels.SmOutput.outputType.GlobalVolumen, ensstats, "");
            //EnsMods.Mat_AddOutputVariable(DtuSmModels.SmOutput.OutputType.outletFlowTimeSeries, "outlet1", true, true, true, true, false);
            //EnsMods.Mat_AddOutputVariableLinkFlow("SM3", "SM4", "myPipe", true, true, true, true, false);

            //EnsMods.AddDA_variable(DtuSmModels.SmOutput.OutputType.outletFlowTimeSeries, "outlet1", "", "", "");


            double[] rainfall = new double[1200];
            rainfall[5] = 5; //5 mm of rainfall in one minute.

            IEnsembleNoise noise = new TemporallyCorrelatedNoise(NumberOfEnsembleMembers, 0.8, 60 * 10, 60, 3);
            EnsMods.AddNoiseOnRain(noise);
            double affald = 0.7;
            Double[] obs = { affald };
            Double[,] obsVar = { { 0.1 } };
            for (int i = 0; i < rainfall.Length / 5; i++)
            {//update every fifth time step. 

                EnsMods.SetRainDataForAllCatchments(rainfall.Skip(i * 5).Take(5).ToArray());
                EnsMods.runForOneMinuteRainInput();
                //EnsMods.StepModels(); EnsMods.StepModels(); EnsMods.StepModels(); EnsMods.StepModels(); EnsMods.StepModels();
                EnsMods.UpdateToObs(obs, obsVar, 1, 1);
            }



            EnsMods.SetRainDataForAllCatchments(rainfall);
            EnsMods.runForOneMinuteRainInput();
            var data1 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[0].data.ToArray();
            var data2 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[1].data.ToArray();
            var data3 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[2].data.ToArray();


        }

        [TestMethod]
        public void TestEnKFupdate()
        {
            var M = Matrix<double>.Build;


            double[,] x = {
                { -2.0, -1.0, 0, 1,  2},
                { -4, -2, 0, 2 , 4 },
                { -40, -20, 0, 20 , 40 }
            };//3 states 5 members
           Matrix<double> E = M.DenseOfArray(x);
            double std1 = MathNet.Numerics.Statistics.Statistics.StandardDeviation(E.Row(1));
            IDaScheme upd = new EnKF();
            
            double[,] HE = {          
                { -4, -2, 0, 2 , 4 },
             };//3 states 1 obs point

            double[] obs = { 0 };
            double[,] obsCoVar = {
                { 0.10}             
            };

           E = upd.Update(E, HE, obs, obsCoVar, 1, 1);
            double std2 = MathNet.Numerics.Statistics.Statistics.StandardDeviation(E.Row(1));

        }
        [TestMethod]
        public void TestEnKFupdateSmartInflate()
        {
            var M = Matrix<double>.Build;


            double[,] x = {
                { -2.0, -1.0, 0, 1,  2},
                { -4, -2, 0, 2 , 4 },
                { -40, -20, 0, 20 , 40 }
            };//3 states 5 members
            Matrix<double> E = M.DenseOfArray(x);
            double std1 = MathNet.Numerics.Statistics.Statistics.StandardDeviation(E.Row(1));
            // IDaScheme upd = new EnKF();
            IDaScheme upd = new EnKFsmartInflation();
            
            double[,] HE = {
                { -4, -2, 0, 2 , 4 },
             };//3 states 1 obs point

            double[] obs = { 1 };
            double[,] obsCoVar = {
                { 0.10}
            };

            E = upd.Update(E, HE, obs, obsCoVar, 1.1, 1);
            double std2 = MathNet.Numerics.Statistics.Statistics.StandardDeviation(E.Row(1));

        }


        [TestMethod]
        public void TestTemporallyCorrelatedNoise()
        {
            int Nsteps = 1000;
            int NumberOfEnsembleMembers = 100;
            double std = 0.3;
            
            IEnsembleNoise noise = new TemporallyCorrelatedNoise(NumberOfEnsembleMembers, std*std, 60 * 10, 60, 3);
            double sampleStd = 0;
            double sampleMean = 0;
            for (int i = 0; i < Nsteps; i++)
            {
                double[] values = noise.DrawNext();
                sampleStd = Math.Sqrt(values.Average(z => z * z) - Math.Pow(values.Average(), 2));
                sampleMean = values.Average();
            }
            Assert.AreEqual( std, sampleStd, 0.1 * std);
            Assert.AreEqual(0, sampleMean, 0.2 * std);

        }


        [TestMethod]
        public void TestCalculateKalmanGain()
        {
            //Test that K is calculated correctly. Using an ensemble of 5 perfectly sorted ensembles for 3 states where HA equals one of the ensembles
            // Two obs. obsvar 10 times bigger for one obs than the other so diff in gain should be 100 amnd hte 

            var M = Matrix<double>.Build;
            double[,] x = {
                { -1.0, -2.0, 0, 2,  1},
                { -4, -2, 0, 2 , 4 },
                { -40, -20, 0, 20 , 40 }
            };//3 states 5 members
            Matrix<double> A = M.DenseOfArray(x);
            double stdxxx = MathNet.Numerics.Statistics.Statistics.StandardDeviation(A.Row(1));


            double[,] xx = {
                 { -4, -2, 0, 2 , 4 },
                 { -4, -2, 0, 2 , 4 }
            };
            Matrix<double> HA = M.DenseOfArray(xx);

            double[,] xxx = {
                { 1.0, 0.0},
                { 0, 100.0}
            };
            Matrix<double> ObsCoVar = M.DenseOfArray(xxx);

            EnsembleModels.KalmanGain KK = new KalmanGain(3, 1);
            KK.CaclulateGain(A, HA, ObsCoVar);
            Assert.AreEqual(0.3604, KK.K[0, 0], 0.01);
            Assert.AreEqual(0.9009, KK.K[1, 0], 0.01);
            Assert.AreEqual(9.0090, KK.K[2, 0], 0.01);
            Assert.AreEqual(0.0036, KK.K[0, 1], 0.01);
            Assert.AreEqual(0.009, KK.K[1, 1], 0.01);
            Assert.AreEqual(00.0901, KK.K[2, 1], 0.01);


            double[] dd = new double[HA.RowCount * HA.ColumnCount];
            MathNet.Numerics.Distributions.Normal.Samples(dd, 0 , 1.0);
            Matrix<double> D = (M.Dense(HA.RowCount, HA.ColumnCount, dd));
            D = D.TransposeThisAndMultiply(ObsCoVar.PointwiseSqrt()).Transpose();
            A = A.Subtract(KK.K.Multiply(HA.Subtract(D)));
            stdxxx = MathNet.Numerics.Statistics.Statistics.StandardDeviation(A.Row(1));
           
        }


        [TestMethod]
        public void TestKalmanUpdateOfParameters()
        {

            //string ParameterFile = "SmallDiverseModel.prm";

            string ParameterFile = "SmallLinResModel.prm";
            int NumberOfEnsembleMembers = 4;
            double initialStdOfParams = 0.2;//relative. 
            double varianceScaling = Math.Sqrt(12 * initialStdOfParams*initialStdOfParams);//since I use a uniform distribution


            string paramsFileFullPath = TestDataFolder + "\\" + ParameterFile;
            EnsembleHandler EnsMods = new EnsembleHandler();
            EnsMods.InitializeFromFile(paramsFileFullPath, NumberOfEnsembleMembers);
            EnsembleStatistics ensstats = new EnsembleStatistics();
            ensstats.SetStats(true, false, true, true, false);
            //EnsMods.AddOutputVariable(DtuSmModels.SmOutput.outputType.GlobalVolumen, ensstats, "");
            EnsMods.Mat_AddOutputVariable(DtuSmModels.SmOutput.OutputType.outletFlowTimeSeries, "outlet1", true, true, true, true, false);


            EnsMods.AddDA_variable(DtuSmModels.SmOutput.OutputType.outletFlowTimeSeries, "outlet1", "", "", "");

            Random rand = new Random();
            for (int i = 0; i < NumberOfEnsembleMembers; i++)
            {
                double[] prm = EnsMods.models[i].getParameters();
                for (int j = 0; j < prm.Length; j++)
                {
                    prm[j] = prm[j]*(1+ (rand.NextDouble() - 0.5) * varianceScaling);
                }
                EnsMods.models[i].setParameter(prm);
            }
            EnsMods.Mat_SetParameterDaMethod("TolDEnKF", 0, 0, 0);


            double[] rainfall = new double[1200];
            rainfall[5] = 5; //5 mm of rainfall in one minute.

            IEnsembleNoise noise = new TemporallyCorrelatedNoise(NumberOfEnsembleMembers, 0.8, 60 * 10, 60, 3);
            EnsMods.AddNoiseOnRain(noise);

            Double[] obs = { 0.7 };
            Double[,] obsVar = { { 0.1 } };
            for (int i = 0; i < rainfall.Length / 5; i++)
            {//update every fifth time step. 

                EnsMods.SetRainDataForAllCatchments(rainfall.Skip(i * 5).Take(5).ToArray());
                EnsMods.runForOneMinuteRainInput();
                EnsMods.updateParamsFromObs(obs, obsVar, 1.001, 0.01); 
                EnsMods.UpdateToObs(obs, obsVar, 1, 1);
            }



            EnsMods.SetRainDataForAllCatchments(rainfall);
            EnsMods.runForOneMinuteRainInput();
            var data1 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[0].data.ToArray();
            var data2 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[1].data.ToArray();
            var data3 = EnsMods.outputCollection.hydraulicOutput[0].dataSeries[2].data.ToArray();
        }



    }
}
