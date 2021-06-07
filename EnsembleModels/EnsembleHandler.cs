using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsembleModels;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Statistics;
using DtuSmModels;


namespace EnsembleModels
{

    public class EnsembleHandler
    {
        public MainModel[] models;

        // private double[] Raindata;
        public int lengthCurrentRain;
        private IEnsembleNoise ensembleNoises;

        public EnsOutputCollection outputCollection;

        private List<EnsembleValuesForPoint> DA_Points;//
        public Matrix<double> E; // working matrix for the update. 

        private IDaScheme Updater;
        private IDaScheme ParameterUpdater;
        private int n_elementsinStateVector;
        protected int m_ensembleMembers;
        private int p_DA_obs;

        public EnsembleHandler()
        {
            //
            this.outputCollection = new EnsOutputCollection();

        }

        public void InitializeFromFile(string parameterFile, int numberOfEnsembleMembers)
        {//if numberOfEnsembleMembers == 0 then read from number prm file. 
            string[,] paramTable = MainModel.getParameterTableFromFile(parameterFile);
            IDictionary<string, int> tableIndexes = parseEnsembleParameters(paramTable);

            if (numberOfEnsembleMembers > 0) m_ensembleMembers = numberOfEnsembleMembers;

            models = new MainModel[m_ensembleMembers];

            for (int i = 0; i < m_ensembleMembers; i++)
            {
                models[i] = new MainModel();
                models[i].createModelInstance(paramTable);
            }
            n_elementsinStateVector = models[0].lengthOfStateVector;
            int ix = 0; //line index in paramTable
            tableIndexes.TryGetValue("[EnsembleOutput]", out ix);
            AddEnsembleOutputFromParameterTable(paramTable, ix);
            tableIndexes.TryGetValue("[DAvariables]", out ix);
            AddDA_variablesFromParameterTable(paramTable, ix);
        }

        private void AddEnsembleOutputFromParameterTable(string[,] paramTable, int ix)
        {
            bool[] ensStats = new bool[5];

            for (int i = ix; i < paramTable.Length; i++)
            {
                if (paramTable[i, 0] == "[EndSect]") return;
                if (paramTable[i, 0] == "<Stats>")
                {
                    string[] split = paramTable[i, 1].Split(new Char[] { ' ', '\t', '=', ',', '(', ')', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    ensStats = new bool[split.Length];
                    for (int j = 0; j < split.Length; j++) ensStats[j] = (split[j] == "1");

                }
                if (paramTable[i, 0] == "<output>")
                {
                    switch (paramTable[i, 1])
                    {

                        case "Flow":
                            Mat_AddOutputVariableLinkFlow(paramTable[i, 2], paramTable[i, 3], paramTable[i, 4], ensStats[0], ensStats[1], ensStats[2], ensStats[3], ensStats[4]);
                            break;
                        case "Vol":
                            Mat_AddOutputVariable(SmOutput.OutputType.nodeVolume, paramTable[i, 2], ensStats[0], ensStats[1], ensStats[2], ensStats[3], ensStats[4]);
                            break;
                        case "outletFlow":
                            Mat_AddOutputVariable(SmOutput.OutputType.outletFlowTimeSeries, paramTable[i, 2], ensStats[0], ensStats[1], ensStats[2], ensStats[3], ensStats[4]);
                            break;
                        case "GlobalVolume":
                            // addOutputVariable(SmOutput.OutputType.GlobalVolumen);
                            throw new NotImplementedException("Unknown connection type: " + paramTable[i, 1]);
                            break;
                        case "NodeWL":
                            SmOutput.OutputType hydraulicType = SmOutput.OutputType.nodeWaterLevel;
                            EnsembleStatistics stats = new EnsembleStatistics();
                            stats.SetStats(ensStats[0], ensStats[1], ensStats[2], ensStats[3], ensStats[4]);
                            EnsOutput xouts = new EnsOutput(stats);
                            xouts.name = paramTable[i, 4];
                            xouts.hydraulicType = hydraulicType;
                            MainModel model = models[0];
                            xouts.nodex = model.getNode((paramTable[i, 2]));
                            xouts.derived = new DerivedValue(paramTable[i, 5]);

                            for (int j = 0; j < models.Length; j++)
                            {
                                SmOutput xout = new SmOutput();
                                xout.name = paramTable[i, 4];
                            }
                            outputCollection.addNewDataSeries(xouts);
                            break;
                        default:
                            throw new NotImplementedException("Unknown connection type: " + paramTable[i, 1]);
                    }
                }

            }

        }

        private void AddDA_variablesFromParameterTable(string[,] paramTable, int ix)
        {
            for (int i = ix; i < paramTable.Length; i++)
            {
                if (paramTable[i, 0] == "[EndSect]") return;

                if (paramTable[i, 0] == "<DAvar>")
                {
                    switch (paramTable[i, 1])
                    {
                        case "Flow":
                            AddDA_variable(SmOutput.OutputType.linkFlowTimeSeries, paramTable[i, 2], paramTable[i, 3], paramTable[i, 4], "");
                            break;
                        case "Vol":
                            AddDA_variable(SmOutput.OutputType.nodeVolume, paramTable[i, 2], paramTable[i, 3], paramTable[i, 4], "");
                            break;
                        case "outletFlow":
                            AddDA_variable(SmOutput.OutputType.outletFlowTimeSeries, paramTable[i, 2], paramTable[i, 3], paramTable[i, 4], "");
                            break;
                        case "NodeWL":
                            AddDA_variable(SmOutput.OutputType.nodeWaterLevel, paramTable[i, 2], "", "", paramTable[i, 5]);
                            break;
                        default:
                            throw new NotImplementedException("Unknown DA point type: " + paramTable[i, 1]);
                    }
                }
            }


            throw new NotImplementedException();
        }

        private IDictionary<string, int> parseEnsembleParameters(string[,] paramTable)
        {
            IDictionary<string, int> tableIndexes = new Dictionary<string, int>();

            System.Globalization.NumberFormatInfo provider = new System.Globalization.NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";

            bool bInEnsembleSection = false;
            int sectionLevel = 0;

            for (int i = 0; i < paramTable.GetLength(0); i++)
            {
                if (paramTable[i, 0] == "[Ensemble]") { bInEnsembleSection = true; sectionLevel++; }

                if (bInEnsembleSection)
                {
                    if (paramTable[i, 0] == "[DAvariables]") { tableIndexes.Add(paramTable[i, 0], i); sectionLevel++; } //bInDAvariablesSection = true;
                    if (paramTable[i, 0] == "[EnsembleOutput]") { tableIndexes.Add(paramTable[i, 0], i); sectionLevel++; } //bInEnsembleOutputSection = true;
                    if (paramTable[i, 0] == "[EndSect]") sectionLevel--;
                    if (sectionLevel == 0) return tableIndexes;

                    if (paramTable[i, 0] == "iNumberOfEnsembleMembers")
                    {
                        try
                        {
                            m_ensembleMembers = Convert.ToInt32(paramTable[i, 1], provider);
                        }
                        catch (Exception ex)
                        {
                            Exception ex2 = new Exception("Error parsing ensemble section", ex);
                            throw ex2;
                        }

                    }


                }
            }
            return tableIndexes;
        }

        public void SetRainDataForAllCatchments(double[] rainfall)
        {
            lengthCurrentRain = rainfall.Length;

            double[] pertNoise = new double[lengthCurrentRain];

            for (int i = 0; i < m_ensembleMembers; i++)
            {
                double[] perturbedRainfall = new double[lengthCurrentRain];
                double[] noise = ensembleNoises.DrawNextValuesForMember(i, lengthCurrentRain);

                for (int j = 0; j < lengthCurrentRain; j++)
                {
                    pertNoise[j] = 1 + noise[j];
                    if (pertNoise[j] < 0) pertNoise[j] = 0;//MB consider removing. 

                    perturbedRainfall[j] = rainfall[j] * pertNoise[j];
                }

                models[i].setRainDataForAllCatchments(perturbedRainfall);

            }

        }

        public void runForOneMinuteRainInput()
        {

            //  models[i].runForOneMinuteRainInput();
            for (int t = 0; t < lengthCurrentRain; t++)
            {
                StepModels();
            }
        }

        public void StepModels()
        {
            for (int i = 0; i < m_ensembleMembers; i++)
            {
                models[i].stepModelWithSetRain();
            }
            CollectOutputData();
        }

        public void UpdateToObs(double[] observations, double[,] obsCoVar, double inflationFactor, double dampningFactor)
        {


            //get forecasted E (XXX = ensemble of state values)
            for (int i = 0; i < m_ensembleMembers; i++)
            {
                E.SetColumn(i, models[i].state.values);
            }

            double[,] HE = GetHE();

            E = Updater.Update(E, HE, observations, obsCoVar, inflationFactor, dampningFactor);


            //set analysis in each model in the ensemble
            for (int i = 0; i < m_ensembleMembers; i++)
            {
                models[i].state.values = E.Column(i).ToArray();
            }

        }


        public double[] GetThreeAffaldDouble()
        {
            double[] affald = new double[] { 1.6, 3.5, 5.7 };
            return affald;
        }


        public double[,] GetHE()
        {
            double[,] HE = new double[p_DA_obs, m_ensembleMembers];
            for (int i = 0; i < p_DA_obs; i++)
            {
                double[] values = DA_Points[i].GetValues();
                for (int j = 0; j < m_ensembleMembers; j++)
                {
                    HE[i, j] = values[j];
                    //hx[i] += values[j];
                }
                //hx[i] = hx[i] / m_ensembleMembers;

            }
            return HE;
        }



        public void updateParamsFromObs(double[] observations, double[,] obsCoVar, double inflationFactor, double dampeningFactor)
        {
            //get params (XXX = ensemble of state values)
            Matrix<double> Eparams = DenseMatrix.Create(models[0].getParameters().Length, models.Length, 0); ; //Could store the params since last update if no continous pertubations are applied


            for (int i = 0; i < m_ensembleMembers; i++)
            {
                Eparams.SetColumn(i, models[i].getParameters());
            }

            double[,] HE = new double[p_DA_obs, m_ensembleMembers];
            //get HE
            for (int i = 0; i < p_DA_obs; i++)
            {
                double[] values = DA_Points[i].GetValues();
                for (int j = 0; j < m_ensembleMembers; j++)
                {
                    HE[i, j] = values[j];
                }
            }

            //IDaScheme upd2 = new EnKFsmartInflation();


            Eparams = ParameterUpdater.Update(Eparams, HE, observations, obsCoVar, inflationFactor, dampeningFactor);


            //set analysis in each model in the ensemble
            for (int i = 0; i < m_ensembleMembers; i++)
            {
                models[i].setParameter(Eparams.Column(i).ToArray());


            }

        }


        public void AddNoiseOnRain(IEnsembleNoise noise)
        {
            ensembleNoises = noise;
        }

        //to make it easy to interface from matlab etc. 
        public void Mat_AddTempCorrNoiseOnRain(double variance, double decorrelationTimeTau, double dt, double truncateLimitInStds)
        {
            ensembleNoises = new TemporallyCorrelatedNoise(m_ensembleMembers, variance, decorrelationTimeTau, dt, truncateLimitInStds);
        }

        public void Mat_updateToObs(double value, double var, double inflationFactor, double dampningFactor)
        {
            Double[] obs = { value };
            Double[,] obsVar = { { var } };
            UpdateToObs(obs, obsVar, inflationFactor, dampningFactor);

        }

        public void Mat_updateToObs(double[] values, double[] vars, double inflationFactor, double dampningFactor)
        { //when multiple DA points. So far no check to whether the DA method supports this. 
            int n = values.Length;
            Double[] obs = new double[n];
            
            Double[,] obsVar = new Double[n, n];
            for (int i = 0; i < n; i++)
            {
                obs[i] = values[i];
                obsVar[i, i] = vars[i];

            }
            UpdateToObs(obs, obsVar, inflationFactor, dampningFactor);

        }

        public void Mat_updateParamsToObs(double value, double var, double inflationFactor, double dampeningFactor)
        {
            Double[] obs = { value };
            Double[,] obsVar = { { var } };
            updateParamsFromObs(obs, obsVar, inflationFactor, dampeningFactor);


        }

        public bool AddOutputVariable(SmOutput.OutputType hydraulicType, EnsembleStatistics stats, string name)
        {

            EnsOutput xouts = new EnsOutput(stats);
            xouts.name = name;
            xouts.hydraulicType = hydraulicType;
            MainModel model = models[0];

            switch (hydraulicType)
            {
                case SmOutput.OutputType.linkFlowTimeSeries:
                    break;
                case SmOutput.OutputType.nodeVolume:
                    xouts.nodex = models[0].getNode(name);
                    break;
                case SmOutput.OutputType.GlobalVolumen:
                    break;
                case SmOutput.OutputType.outletFlowTimeSeries:

                    bool bsuccess = false;
                    foreach (int i in model.iOutlets)
                    {
                        if (model.Nodes[i].name == name)
                        {
                            xouts.outletx = new Outlet[models.Length];


                            for (int j = 0; j < models.Length; j++)
                            {
                                xouts.outletx[j] = (Outlet)models[j].Nodes[i];
                            }
                            bsuccess = true;

                        }
                    }
                    if (!bsuccess) throw new Exception("could not find output variable " + name);
                    break;
                default:
                    break;
            }

            outputCollection.addNewDataSeries(xouts);
            return true;
        }

        //to make it easy to interface from matlab etc. 
        public bool Mat_AddOutputVariable(SmOutput.OutputType hydraulicType, string name, bool mean, bool std, bool min, bool max, bool median)
        {
            EnsembleStatistics stats = new EnsembleStatistics();
            stats.SetStats(mean, std, min, max, median);
            AddOutputVariable(hydraulicType, stats, name);
            return true;
        }


        public bool Mat_AddOutputVariableLinkFlow(string fromNode, string toNode, string name, bool mean, bool std, bool min, bool max, bool median)
        {
            SmOutput.OutputType hydraulicType = SmOutput.OutputType.linkFlowTimeSeries;
            EnsembleStatistics stats = new EnsembleStatistics();
            stats.SetStats(mean, std, min, max, median);
            EnsOutput xouts = new EnsOutput(stats);
            xouts.name = name;
            xouts.hydraulicType = hydraulicType;
            MainModel model = models[0];
            List<Connection> cons = model.getConnections();
            int fromIndex = model.getNode(fromNode).index;
            int toIndex = model.getNode(toNode).index;

            bool bsuccess = false;
            for (int i = 0; i < cons.Count; i++)
            {

                if (cons[i].from == fromIndex && cons[i].to == toIndex)
                {
                    SmOutput xout = new SmOutput();
                    xout.name = name;
                    xouts.con = new Connection[models.Length];


                    for (int j = 0; j < models.Length; j++)
                    {
                        xouts.con[j] = models[j].Connections[i];
                    }
                }

                bsuccess = true;


            }
            if (!bsuccess) throw new Exception("could not find output variable " + name);

            outputCollection.addNewDataSeries(xouts);
            return true;
        }

        public bool Mat_StateDaMethod(string name, double param1, double param2, double param3)
        {
            Updater = GetDaMethod(name);
            return true;
        }

        public bool Mat_SetParameterDaMethod(string name, double param1, double param2, double param3)
        {
            ParameterUpdater = GetDaMethod(name);
            return true;
        }

        private IDaScheme GetDaMethod(string name)
        {
            IDaScheme updx;
            switch (name)
            {
                case "EnKF":
                    updx = new EnKF();
                    break;

                case "EnKFsmartInflation":
                    updx = new EnKFsmartInflation();
                    break;
                case "DEnKF":
                    updx = new DEnKF();
                    break;
                case "TolDEnKF":
                    updx = new TolDEnKF();
                    break;
                case "TolDEnKFdamp":
                    updx = new TolDEnKFdamp();
                    break;
                default:
                    throw new Exception("DA method: " + name + " does not exist");

            }

            return updx;
        }

        public void Mat_perturbeParameters(double std, double truncationLimInStd)
        {
            Random rand = new Random();
            double varianceScaling = Math.Sqrt(12 * std * std);//since I use a uniform distribution
            double truncateLimit = truncationLimInStd * std;

            for (int i = 0; i < m_ensembleMembers; i++)
            {
                double[] prm = models[i].getParameters();
                for (int j = 0; j < prm.Length; j++)
                {
                    double factor = (1 + (rand.NextDouble() - 0.5) * varianceScaling);
                    if (factor > (1 + truncateLimit)) factor = (1 + truncateLimit);
                    else if (factor < (1 - truncateLimit)) factor = 1 - truncateLimit;


                    prm[j] = prm[j] * factor;
                }
                models[i].setParameter(prm);
            }


        }

        public void AddDA_variable(SmOutput.OutputType outputType, string nodeOrOutlet, string toNode, string name, string additionalParams)
        {
            if (DA_Points == null)
            {
                DA_Points = new List<EnsembleValuesForPoint>();
                E = DenseMatrix.Create(models[0].state.values.Length, models.Length, 0);
                //Updater = new EnKFsmartInflation(); 
                Updater = new EnKF();
            }

            EnsembleValuesForPoint DApoint;
            switch (outputType)
            {
                case SmOutput.OutputType.outletFlowTimeSeries:
                case SmOutput.OutputType.nodeWaterLevel:
                    DApoint = new EnsembleValuesForPoint(models, outputType, nodeOrOutlet, additionalParams);
                    break;
                default:
                    throw new NotImplementedException();
            }
            DA_Points.Add(DApoint);
            p_DA_obs++;
        }

        private void CollectOutputData()
        {
            outputCollection.timeInSeconds.Add(models[0].t);
            foreach (EnsOutput ensOut in outputCollection.hydraulicOutput)
            {
                SmOutput.OutputType hydraulicType = ensOut.hydraulicType;

                double[] ensValues = new double[m_ensembleMembers];

                switch (hydraulicType)
                {
                    case SmOutput.OutputType.linkFlowTimeSeries:
                        for (int i = 0; i < m_ensembleMembers; i++)
                        {
                            ensValues[i] = ensOut.con[i].retrieveMeanFlow();
                        }
                        break;
                    case SmOutput.OutputType.nodeVolume:
                        for (int i = 0; i < m_ensembleMembers; i++)
                        {
                            ensValues[i] = models[i].state.values[ensOut.nodex.index];
                        }
                        break;
                    case SmOutput.OutputType.GlobalVolumen:

                        for (int i = 0; i < m_ensembleMembers; i++)
                        {
                            ensValues[i] = models[i].state.values.Sum();
                        }

                        break;
                    case SmOutput.OutputType.outletFlowTimeSeries:
                        for (int i = 0; i < m_ensembleMembers; i++)
                        {
                            ensValues[i] = ensOut.outletx[i].flow;
                        }

                        break;
                    case SmOutput.OutputType.nodeWaterLevel:
                        try
                        {
                            for (int i = 0; i < m_ensembleMembers; i++)
                            {
                                ensValues[i] = ensOut.derived.calculate(models[i].state.values[ensOut.nodex.index]);
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Error collecting derived WL output for node " + ensOut.name + ": " + e.Message);
                        }

                        break;
                    default:
                        break;
                }


                int j = 0;
                foreach (SmOutput.StatType statType in ensOut.stats.types)
                {

                    switch (statType)
                    {
                        case SmOutput.StatType.deterministic:
                            throw new Exception("Error collecting output");
                            break;
                        case SmOutput.StatType.mean:
                            ensOut.dataSeries[j].updateData(ensValues.Average());
                            break;
                        case SmOutput.StatType.std:
                            double average = ensValues.Average();
                            double sumOfSquaresOfDifferences = ensValues.Select(val => (val - average) * (val - average)).Sum();
                            double std = Math.Sqrt(sumOfSquaresOfDifferences / ensValues.Length);
                            ensOut.dataSeries[j].updateData(std);
                            break;
                        case SmOutput.StatType.min:
                            ensOut.dataSeries[j].updateData(ensValues.Min());
                            break;
                        case SmOutput.StatType.max:
                            ensOut.dataSeries[j].updateData(ensValues.Max());
                            break;
                        case SmOutput.StatType.median:
                            ensOut.dataSeries[j].updateData(ensValues.Median());
                            break;
                        case SmOutput.StatType.accumulated:
                            throw new Exception("Error collecting output");
                            break;
                        default:
                            throw new Exception("Error collecting output");
                            break;
                    }
                    j++;

                }



            }
        }
    }
}


