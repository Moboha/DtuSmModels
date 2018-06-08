using Microsoft.VisualStudio.TestTools.UnitTesting;
using DtuSmModels;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtuSmModels.Tests
{
    [TestClass()]
    public class MainModelTests
    {
        string TestDataFolder = @"H:\SyncPC\csharp\SurrogateModels\DtuSmModels\TestData"; //
        //string TestDataFolder = @"R:\Research Communities\SurrogateModelling\Data\TestData";



        [TestMethod()]
        public void MainModelTest()
        {
            MainModel model = new MainModel();
            Assert.IsNotNull(model);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod()]
        public void initializeFromFileTest()
        {
            string parameterFile = "ParametersVers2.prm";
            MainModel model = new MainModel();
            model.initializeFromFile(TestDataFolder + "\\" + parameterFile);


            Assert.IsFalse(false);


            model.saveModelParameters(TestDataFolder + "\\" + "autoWrittenPrm.Prm");

        }

        [TestMethod()]
        public void modelStepTest()
        {
            string parameterFile = "SmallPieceWiseLinResModel.prm";
            //string parameterFile = "SmallLinResModel.prm";
            //string parameterFile = "LargeModel.prm";

            MainModel model = new MainModel();
            model.initializeFromFile(TestDataFolder + "\\" + parameterFile);
            Random rnd = new Random();

            // double[] init = new double[]{ 100.0, 100, 0, 0,};
            double[] init = new double[model.state.values.Length];
            double[] forcing = new double[init.Length];
            // forcing[0] = 12; forcing[4] = 1;//investigate why model is slower whith no input.
            model.setInitialCond(init);
            while (model.t < 24 * 360 * 60 * 1)
            {
                forcing[0] =  rnd.NextDouble();
                model.modelStep(60, forcing);
                //  Debug.WriteLine("sdf");
                // var A = model.volumes;
                //Console.WriteLine("to console2 " + A[0] + " " + A[1] + " " + A[2] + " " + A[3]);

            }
            Console.Write("number of states:" + model.state.values.Length + " states:");



            foreach (double x in model.state.values) Console.Write(x + " ");

            // Console.WriteLine(model.volumes[0] + " " + model.volumes[1] + " " + model.volumes[2] + " " + model.volumes[3]);
            Assert.IsFalse(false);
        }

        [TestMethod()]
        public void runForOneMinuteSingleRainInputTest()
        {
            string parameterFile = "LargeModel.prm";
            MainModel model = new MainModel();

           // model.setOutFile(@"R:\Research Communities\SurrogateModelling\Matlab\Tests\output\affaldbb.txt");

            model.initializeFromFile(TestDataFolder + "\\" + parameterFile);
            double[] rainfall = new double[120];
            rainfall[5] = 5; //5 mm of rainfall in one minute.

            //model.setRainData(rainfall);
            //foreach (var x in rainfall) model.stepModelWithSetRain();
            model.addOutputVariable("aaaSM2", "aaaSM3","");
            model.addOutputVariable("bbbSM6", "bbbSM7","");
            model.addOutputVariable("bbbSM6", SmOutput.outputType.nodeVolume);

            model.setRainDataForAllCatchments(rainfall);
            model.runForOneMinuteRainInput();


            foreach (double x in model.state.values) Console.Write(x + " ");
            Console.WriteLine("\ndata:");

            foreach(double x in model.output.dataCollection[1].data) Console.Write(x + " ");


        }


        [TestMethod()]
        public void runForOneMinuteSingleRainInputTest2()
        {
            string parameterFile = "SmallDiverseModel.prm";
            MainModel model = new MainModel();

            model.initializeFromFile(TestDataFolder + "\\" + parameterFile);
            double[] rainfall = new double[120];
            rainfall[5] = 5; //5 mm of rainfall in one minute.

            model.setRainDataForAllCatchments(rainfall);
            model.runForOneMinuteRainInput();



        }

        [TestMethod()]
        public void forecastTest1()
        {
            string parameterFile = "SmallDiverseModel.prm";
            MainModel model = new MainModel();

            model.initializeFromFile(TestDataFolder + "\\" + parameterFile);
            double[] rainfall = new double[120];
            rainfall[5] = 5; //5 mm of rainfall in one minute.
            model.setRainDataForAllCatchments(rainfall);
            model.runForOneMinuteRainInput();
            model.setInitialCond(model.state.values);
            model.setRainDataForAllCatchments(rainfall);
            model.runForOneMinuteRainInput();

        }


        [TestMethod()]
        public void forecastTest2()
        {
            string parameterFile = "LinResSurfaceModelTest.prm";
            MainModel model = new MainModel();

            model.initializeFromFile(TestDataFolder + "\\" + parameterFile);
            double[] rainfall = new double[100];
            rainfall[5] = 5; //5 mm of rainfall in one minute.
            
            model.addOutputVariable("SM1", "SM2", "");
            model.addOutputVariable("SM2", "SM3", "");

            model.setRainDataForAllCatchments(rainfall);
            model.runForOneMinuteRainInput();

            double[] initSurf = model.getSurfaceModuleStates();
            double[] initStates = model.state.values;



            model.setSurfaceModuleStates(initSurf);
            model.setInitialCond(initStates);
            model.output.resetOutputSeries();
            model.setRainDataForAllCatchments(rainfall);
            model.runForOneMinuteRainInput();
            var data1 = model.output.dataCollection[0].data.ToArray();
            int Lx1 = data1.Count();

            model.setSurfaceModuleStates(initSurf);
            model.setInitialCond(initStates);
            model.output.resetOutputSeries();
            model.setRainDataForAllCatchments(rainfall);
            model.runForOneMinuteRainInput();
            var data2 = model.output.dataCollection[0].data.ToArray();
            int Lx2 = data1.Count();

            Assert.IsTrue(Lx1 == Lx2);
            Assert.IsTrue(data1.ToArray().SequenceEqual(data2.ToArray()));



        }

        [TestMethod()]
        public void AffaldxxTest()
        {
            //string parameterFile = TestDataFolder + "\\" + "TrojBas1_orig.prm";
            //string parameterFile = @"H:\SyncPC\csharp\SurrogateModels\DtuSmModels\TestData\trojBas2_maxvol_red.PRM";
            string parameterFile = "not a prm file";

            MainModel model = new MainModel();

            // model.setOutFile(@"R:\Research Communities\SurrogateModelling\Matlab\Tests\output\affaldbb.txt");
            try
            {
                model.initializeFromFile(parameterFile);
            }
            catch (Exception)
            {
                return;
            }
            throw new Exception("not supposed to reach this line");

        }



        [TestMethod()]
        public void affaldxxTest2()
        {
            string parameterFile = "SmallLinResAndUnitHydroModel.PRM";
            MainModel model = new MainModel();

            // model.setOutFile(@"R:\Research Communities\SurrogateModelling\Matlab\Tests\output\affaldbb.txt");

            model.initializeFromFile(TestDataFolder + "\\" + parameterFile);
            List<Connection>conns = model.getConnections();
            double[] myparams = conns[1].getParameterArray();
            double[] newparams = new double[] { 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0 };
            conns[1].setParameters(newparams);
            myparams = conns[1].getParameterArray();

            double[] rainfall = new double[120];
            rainfall[5] = 1; //50 mm of rainfall in one minute.

            model.setRainDataForAllCatchments(rainfall);
            model.runForOneMinuteRainInput();


            foreach (double x in model.state.values) Console.Write(x + " ");
            Console.WriteLine("\ndata:");

            // foreach (double x in model.output.dataCollection[0].data) Console.Write(x + " ");
      //      model.saveModelParameters(TestDataFolder + "\\" + "autoWrittenPrm3.Prm");

        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod()]
        public void getAndSetParameters()
        {
            //string parameterFile = "SmallPieceWiseLinResModel.prm";
            string parameterFile = "SmallDiverseModel.prm";
            MainModel model = new MainModel();
            model.initializeFromFile(TestDataFolder + "\\" + parameterFile);
            double[] parameters = model.getParameters();
            foreach (double x in parameters) Console.Write(x + " ");

            var newParams = new double[32];
            int i= 0;
            foreach (double x in parameters)
            {
                newParams[i] = 1.5 * x;
                i++;
            }
            model.setParameter(newParams);
            model.saveModelParameters(TestDataFolder + "\\" + "autoWrittenPrm4.Prm");

            double[] parameters2 = model.getParameters();
            Console.WriteLine("");
            foreach (double x in parameters2) Console.Write(x + " ");

            

            Assert.IsFalse(false);
        }



    }

}