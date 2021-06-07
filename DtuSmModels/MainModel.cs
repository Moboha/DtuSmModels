using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DtuSmModels
{
    
    public static class myconst
    {
        public const double DT = 60; //time step in seconds
    }

    public class MainModel : IMainModel
    {   const double virtuallyZero = 0.00000000001;// used to avoid wierd values with both denominator and counters approax zero. 

        //public static double TimeStepInSeconds { get; } = 60;
        public List<Node> Nodes => nodes;

        private List<Node> nodes;
        private List<Connection> connections;
        private List<Catchment> catchments;
        private List<FlowDivider> flowDividers;
        public int[] iOutlets; //index of outlets in the state vector.

        //private double[] dydt; //change to state vector
        private double[] qSplits; // flows in splitters. NOT part of the volume state vector.
        private double[] qMassSplits; //flux of mass in splitters

        public int lengthOfStateVector;
        public int NhydraulicStates; //used when including WQ calculations wich doubles the length of the statevector.
        public double t; //in seconds
        private RainfallData raindata;
        private List<RainfallData> individualRainDatas;

        public int RainfallDataLength => raindata.data.Length;
        private int lenghtOfRainfallData;
        public StateVector state;
        private bool bIncludeWQ;

        private System.IO.StreamWriter logFile; //affald - slet igen senere. 
        public SmOutputCollection output;
        private Solver mySolver;

        public MainModel()
        {
            //Console.Write("New Surrogate model");
            Compartment.resetNumberOfCompartments();
            FlowDivider.resetNumberOfFlowDividers();
            raindata = new RainfallData();
            this.output = new SmOutputCollection();
            this.bIncludeWQ = false;
        }

        public void modelStep(double dt, double[] forcing)
        {
            state.values = mySolver.solve(dt, state.values, forcing);
            foreach (int i in iOutlets)
            {
                ((Outlet)nodes[i]).flow = state.values[i] / myconst.DT;
                state.values[i] = 0;
            }

            t += dt;
        }

        public void setInitialCond(double[] init)
        {
            state.values = init;
        }

        public void includeWQcalculation()
        {
            bIncludeWQ = true;
            NhydraulicStates = lengthOfStateVector;
            lengthOfStateVector = lengthOfStateVector * 2;
            this.state.values = new double[lengthOfStateVector];
        }

        internal double[] calculateDyDt(double[] vols, double[] forcing)
        {   

            //System.Array.Clear(dydt,0,lengthOfStateVector);
            double[] tempDyDt = new double[lengthOfStateVector];

            for (int i = 0; i < vols.Length; i++) //MB: Constraining to volumes above or equal to zero.
            {
                if (vols[i] < 0) vols[i] = 0;
            }


            for (int i = 0; i < qSplits.Length; i++)
            {
                qSplits[i] = 0;
                qMassSplits[i] = 0;
            }
            foreach (Connection con in connections)
            {
                try
                {                                    
                    if (!con.bFromFlowDivider)
                    {
                        double q = con.calculateFlow(vols);
                        tempDyDt[con.from] -= q;
                        
                        double qMass = 0;
                        if (bIncludeWQ)
                        {
                            if (vols[con.from]< virtuallyZero)
                            {
                                qMass = 0;
                            }
                            else 
                            {
                                qMass = q * vols[con.from + NhydraulicStates] / vols[con.from];//mass flux calculated as flow times mass / volume;
                            }
                            
                            tempDyDt[con.from + NhydraulicStates] -= qMass;
                        }

                        
                        if (con.bToFlowDivider)
                        {
                            this.qSplits[con.to] += q;//add flow to the right flow divider. 
                            if (bIncludeWQ) this.qMassSplits[con.to] += qMass; 
                        }
                        else
                        {
                            if (con.to != System.Int32.MaxValue) //outlets are given MaxValue as magic number.
                            {   
                                tempDyDt[con.to] += q;
                                if (bIncludeWQ) tempDyDt[con.to + NhydraulicStates] += qMass;

                            }
                        }
                        if (con.bIsOutput)
                        {
                            con.accumFlow(q);//for calculating output flow as avereage of all solver steps. 
                            if (bIncludeWQ) con.accumMassFlux(qMass);
                        }                           
                    }
                }
                catch (Exception ex1)
                {
                    Exception ex2 = new Exception("Time t=: " + t + " Error calculating flow: " + nodes[con.from].name + " to " + nodes[con.to].name + "   vol in from node=" + vols[con.from], ex1);
                    throw ex2;
                }  
            }

            foreach (FlowDivider fd in flowDividers)
            {
                //if (bIncludeWQ) throw new NotImplementedException("WQ not implemented for flow dividers yet");
                int Ncons = fd.connections.Count;
                double[] qs = new double[Ncons];
                int[] stateVectorIndexes = new int[Ncons];
                int i = 0;
                foreach (FlowDividerConnection con in fd.connections)
                {//calculate flow from all flow dividers. The flow to has been calculated above.                   
                    qs[i] = con.calculateFlow(qSplits);
                    if (con.to != System.Int32.MaxValue) //outlets are given MaxValue as magic number.
                    {   
                        tempDyDt[con.to] += qs[i];
                        stateVectorIndexes[i] = con.to;
                    }
                    if (con.bIsOutput) con.accumFlow(qs[i]);//for calculating output flow as avereage of all solver steps. 

                    i++;
                }
                if (bIncludeWQ)
                {
                    double sumx = qs.Sum();//total flow from divider.
                    double qMassInTotal = qMassSplits[fd.index]+ fd.qMassFixed;
                    for (int j = 0; j < Ncons; j++)
                    {
                        double qMassx;
                        if (sumx < virtuallyZero)
                        {
                            qMassx = 0;
                        }
                        else
                        {
                            qMassx =   qMassInTotal * qs[j] / sumx;
                        }
                        
                        
                        tempDyDt[stateVectorIndexes[j] + NhydraulicStates] += qMassx;
                        if (fd.connections[j].bIsOutput) fd.connections[j].accumMassFlux(qMassx);
                    }
                }
                  
            }       

            tempDyDt = RungeKutta4.arrSum(tempDyDt, forcing);
            return tempDyDt;
        }

        public void runForOneMinuteRainInput()
        {
            int i = 0;
            try
            {
                for (i = 0; i < lenghtOfRainfallData; i++)
                {
                    stepModelWithSetRain(1);
                    //collectOutputData();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error at time step " + i + ": " + e.Message);
            }
        }

        private void collectOutputData()
        {
            output.timeInSeconds.Add(t);
            foreach (SmOutput xout in output.dataCollection)
            {
                if (xout.type == SmOutput.OutputType.linkFlowTimeSeries)
                {
                    //  xout.updateData(xout.con.calculateFlow(state.values));
                    xout.updateData(xout.con.retrieveMeanFlow());
                }
                else if (xout.type == SmOutput.OutputType.outletFlowTimeSeries)
                {
                    xout.updateData(xout.outletx.flow);
                }
                else if (xout.type == SmOutput.OutputType.nodeVolume)
                {
                    xout.updateData(state.values[xout.nodex.index]);
                }
                else if (xout.type == SmOutput.OutputType.nodeWaterLevel)
                {
                    try
                    {
                        xout.updateData(xout.derived.calculate(state.values[xout.nodex.index]));
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Error collecting derived WL output for node " + xout.name + ": " + e.Message + "\n The volume in the compartment was: " + state.values[xout.nodex.index]);
                    }

                }
                else if (xout.type == SmOutput.OutputType.WQmassFlux)
                {
                    xout.updateData(xout.con.retrieveMeanMassFlux());
                }
                else if (xout.type == SmOutput.OutputType.GlobalVolumen)
                {
                    xout.updateData(state.values.Sum());
                }               
                else
                {
                    throw new Exception("Error collecting output");
                }

            }
        }

        public void setRainDataForAllCatchments(double[] rainInMmOneMinSteps) //Only apply to catchments where individualRainData has not been assigned.
        {
            raindata.setRainData(rainInMmOneMinSteps);

            foreach (Catchment catx in catchments)
            {
                //if (catx is TA1) ((TA1)catx).setRainfallData(raindata);
                catx.setRainfallData(raindata);
            }

            t = 0;
            lenghtOfRainfallData = raindata.data.Count();
        }

        public void stepModelWithSetRain(int numberOfSteps = 1)
        {
            for (int i = 0; i < numberOfSteps; i++)
            {
                double[] forcingVector = new double[state.values.Length];
                foreach (Catchment cat in catchments)
                {
                    if (cat.node.bHasVolume)
                    {
                        forcingVector[cat.node.index] += cat.getNextFlowInM3PrS();
                        if (cat.bHasAdditionalFlow)
                        {
                            forcingVector[cat.node.index] += cat.qAdd;
                            if (bIncludeWQ)
                            {
                                forcingVector[cat.node.index + NhydraulicStates] += cat.qAdd * cat.concIn_qAdd;// can reduce computational time by only calculating this flux in the catchment whenever flow or conc is changed.
                            }
                        }
                    }
                    else
                    {
                        ((FlowDivider)cat.node).qFixed = cat.getNextFlowInM3PrS();//WARNING -TODO:  This means flow dividers only work with a sinlge catchment.
                        if (cat.bHasAdditionalFlow)
                        {
                            ((FlowDivider)cat.node).qFixed += cat.qAdd;
                            if (bIncludeWQ) ((FlowDivider)cat.node).qMassFixed = cat.qAdd * cat.concIn_qAdd;//WARNING -TODO:  This means flow dividers only work with a sinlge catchment
                        }
                    }

                }

                modelStep(myconst.DT, forcingVector);

                collectOutputData();
            }
        }

        public void initializeFromFile(string parameterFileFullPath)
        {
            string[,] paramTable = getParameterTableFromFile(parameterFileFullPath);
            createModelInstance(paramTable);

            if (logFile != null)
            {
                logFile.WriteLine(parameterFileFullPath);
                logFile.WriteLine("nodes: " + nodes.Count());
                logFile.WriteLine("connections: " + connections.Count());
                logFile.WriteLine("catchments: " + catchments.Count());
            }
        }

        //public string[,] initializeFromFileAndReturnParamTable(string parameterFileFullPath)
        //{
        //    string[,] paramTable = getParameterTableFromFile(parameterFileFullPath);
        //    createModelInstance(paramTable);
        //    return paramTable;

        //}


        public void createModelInstance(string[,] paramTable)
        {
            this.flowDividers = new List<FlowDivider>();
            this.nodes = instantiateNodes(paramTable, this.flowDividers);
            this.connections = getConnections(paramTable);
            this.catchments = getCatchments(paramTable);


            lengthOfStateVector = ((Compartment)nodes[0]).totalNumberOfCompartments();

            this.state = new StateVector();
            this.state.values = new double[lengthOfStateVector];
          // this.dydt = new double[lengthOfStateVector]; //TODO - is it even used?
            this.qSplits = new double[flowDividers.Count];
            this.qMassSplits = new double[flowDividers.Count];
            // Connection.state = this.state;
            var xioutlets = new List<int>();
            foreach (Node n in nodes)
            {
                if (n is Outlet) xioutlets.Add(n.index);
            }

            this.iOutlets = xioutlets.ToArray();
            this.mySolver = new RungeKutta4adaptive(this);


            if (!ensembleOutputOnly(paramTable)) addOutputVariablesFromParameters(paramTable);
        }

        private bool ensembleOutputOnly(string[,] paramTable)
        {
            bool bOnlyEnsembleOutput = false;
            bool bInEnsembleSection = false;

            for (int i = 0; i < paramTable.GetLength(0); i++)
            {
                if (paramTable[i, 0] == "[Ensemble]") bInEnsembleSection = true;

                if (bInEnsembleSection)
                {
                    if (paramTable[i, 0] == "bEnsembleRun")
                    {
                        if (paramTable[i, 2] == "0") return false;
                    }
                    if (paramTable[i, 0] == "bSingleModelsOutput")
                    {
                        if (paramTable[i, 2] == "1") return false;
                    }
                }
            }

            return bOnlyEnsembleOutput;
        }

        private void addOutputVariablesFromParameters(string[,] paramTable)
        {
            {
                bool bInOutputSection = false;

                for (int i = 0; i < paramTable.GetLength(0); i++)
                {
                    if (paramTable[i, 0] == "[Output]") bInOutputSection = true;

                    if (bInOutputSection)
                    {
                        if (paramTable[i, 0] == "[EndSect]") bInOutputSection = false;
                        else
                        {

                            while (paramTable[i, 0] == "<output>")
                            {


                                switch (paramTable[i, 1])
                                {
                                    case "Flow":
                                        addOutputVariable(paramTable[i, 2], paramTable[i, 3], paramTable[i, 4]);
                                        break;
                                    case "Vol":
                                        addOutputVariable(paramTable[i, 2], SmOutput.OutputType.nodeVolume);
                                        break;
                                    case "outletFlow":
                                        addOutputVariable(paramTable[i, 2]);
                                        break;
                                    case "GlobalVolume":
                                        addOutputVariable(SmOutput.OutputType.GlobalVolumen);
                                        break;
                                    case "NodeWL":
                                        SmOutput xout = new SmOutput();
                                        xout.type = SmOutput.OutputType.nodeWaterLevel;
                                        xout.name = paramTable[i, 4];
                                        xout.nodex = getNode(paramTable[i, 2]);
                                        xout.derived = new DerivedValue(paramTable[i, 5]);
                                        output.addNewDataSeries(xout);
                                        break;
                                    default:
                                        throw new NotImplementedException("Unknown connection type: " + paramTable[i, 1]);
                                }

                                i++;
                            }
                        }

                    }
                }
            }

        }

        public void setAdditionalFlowPerUnit(double flowPrUnit, double concentration, int profileNumber)
        {//profileNumber is an integer from 1 and up that decides which catchments is effected by the method. Flow is in m3/s and conc in kg/m3 (=g/l)
           

            foreach (Catchment cat in catchments)
            {
                if (cat.bHasAdditionalFlow)
                {
                    if(cat.profileIndex == profileNumber)
                    {
                        cat.qAdd = flowPrUnit * cat.numberOfUnits;
                        cat.concIn_qAdd = concentration;
                    }
                }
            }

        }

        public void setParameter(double[] newParameters)
        {
            if (logFile != null) logFile.WriteLine("setParameters() " + newParameters.Count());
            double[] locParams = new double[newParameters.Count()];

            for (int j = 0; j < newParameters.Count(); j++)
            {
                if (logFile != null) logFile.WriteLine("j=" + j + " " + newParameters[j]);
                locParams[j] = newParameters[j];
            }

            int i = 0;
            foreach (Connection con in connections)
            {
                if (logFile != null) logFile.WriteLine("setParameters() i=" + i);
                // i += con.setParameterArray(newParameters, i);

                i += con.setParameterArray(locParams, i);
                if (logFile != null) logFile.WriteLine(" i=" + i);
            }

            foreach (Catchment cat in catchments)
            {
                if (logFile != null) logFile.WriteLine("setParameters() i=" + i);
                // i += con.setParameterArray(newParameters, i);

                i += cat.setParameterArray(locParams, i);
                if (logFile != null) logFile.WriteLine(" i=" + i);
            }
        }

        public bool setIndividualRainData(string catchmentNode, double[] oneMinuteRainfallx)
        {
            //asdfasdfasfdasdf ikke tested endnu.
            bool success = false;

            if (lenghtOfRainfallData == 0)
            {
                lenghtOfRainfallData = oneMinuteRainfallx.Length;
            }
            else
            {
                if (lenghtOfRainfallData != oneMinuteRainfallx.Length)
                    throw new Exception("The various rainfall data do not have the same number of data points");
            }


            if (individualRainDatas == null)
            {
                individualRainDatas = new List<RainfallData>();
            }

            RainfallData rainDatax = new RainfallData();
            rainDatax.setRainData(oneMinuteRainfallx);
            individualRainDatas.Add((rainDatax));
            foreach (var cat in catchments)
            {
                if (cat.node.name.Equals(catchmentNode))
                {
                    cat.setRainfallData(rainDatax);
                    success = true;
                    break;
                }
            }

            t = 0;
            return success;
        }

        private List<Catchment> getCatchments(string[,] paramTable)
        {
            var xcatchments = new List<Catchment>();

            bool bInRunoffSection = false;
            bool bInSurfaceModelsSection = false;

            for (int i = 0; i < paramTable.GetLength(0); i++)
            {
                if (paramTable[i, 0] == "[Runoff]") bInRunoffSection = true;

                if (bInRunoffSection)
                {
                    if (paramTable[i, 0] == "[SurfaceModels]") bInSurfaceModelsSection = true;

                    if (bInSurfaceModelsSection)
                    {
                        if (paramTable[i, 0] == "[EndSect]")
                        {
                            bInSurfaceModelsSection = false;
                            i++;
                        }
                        try
                        {
                            while (paramTable[i, 0] == "<SurfMod>")
                            {
                                Catchment catx;
                                switch (paramTable[i, 2])
                                {
                                    case "TA1":
                                        catx = new TA1(getNodeOrDivider(paramTable[i, 1]), paramTable[i, 3]);
                                        break;
                                    case "LinResSurf2":
                                        catx = new LinResSurf2(getNodeOrDivider(paramTable[i, 1]), paramTable[i, 3]);
                                        break;
                                    case "PlainArea":
                                        catx = new PlainArea(getNodeOrDivider(paramTable[i, 1]), paramTable[i, 3]);
                                        break;
                                    default:
                                        throw new Exception("Error constructing surface model. Unknown SurfModel type: " + paramTable[i, 2]);
                                }
                                catx.setRainfallData(raindata);

                                if(paramTable[i, 4] != null)
                                {
                                    catx.bHasAdditionalFlow = true;

                                    System.Globalization.NumberFormatInfo provider = new System.Globalization.NumberFormatInfo();
                                    provider.NumberDecimalSeparator = ".";
                                    string[] split = paramTable[i, 4].Split(new Char[] { ' ', '\t', '=', ',', '(', ')', ';' }, StringSplitOptions.RemoveEmptyEntries);
                                    catx.numberOfUnits = Convert.ToDouble(split[0], provider);
                                    catx.profileIndex = Convert.ToInt32(split[1], provider);

                                }
                                xcatchments.Add(catx);
                                i++;
                            }
                        }

                        catch (Exception e)
                        {
                            throw new Exception("Error constructing surface model: " + e.Message);
                        }

                    }

                    if (paramTable[i, 0] == "[EndSect]")
                    {
                        bInRunoffSection = false;
                        i++;
                    }
                }
            }

            return xcatchments;
        }

        private List<Connection> getConnections(string[,] paramTable)
        {
            {
                var connections = new List<Connection>();
                bool bInHydraulicSection = false;

                for (int i = 0; i < paramTable.GetLength(0); i++)
                {
                    if (paramTable[i, 0] == "[HydraulicModel]") bInHydraulicSection = true;

                    if (bInHydraulicSection)
                    {
                        if (paramTable[i, 0] == "[EndSect]") bInHydraulicSection = false;
                        else
                        {
                            //instantiate all connections
                            if (paramTable[i, 0] == "<name>")
                            {
                                if (paramTable[i+1, 1] == "drainage")
                                {
                                    {
                                        Node fromComp = getNode(paramTable[i, 1]);

                                        i = i + 2;
                                        while (paramTable[i, 0] == "<connection>")
                                        {
                                            Node toComp = getNodeOrDivider(paramTable[i, 1]);
                                            Connection conx;
                                            switch (paramTable[i, 2])
                                            {
                                                case "LinRes":
                                                    conx = new LinRes(fromComp.index, toComp.index, paramTable[i, 3]);
                                                    break;
                                                case "LinResWithMax":
                                                    conx = new LinResWithMax(fromComp.index, toComp.index, paramTable[i, 3]);
                                                    break;
                                                case "PieceWiseLinRes":
                                                    conx = (new PieceWiseLinRes(fromComp.index, toComp.index, paramTable[i, 3]));
                                                     break;
                                                case "LinResWithMaxAndBackWater":
                                                    conx = (new LinResWithMaxAndBackWater(fromComp.index, toComp.index, paramTable[i, 3]));
                                                    break;
                                                case "SpillingVolume":
                                                    conx = (new SpillingVolume(fromComp.index, toComp.index, paramTable[i, 3]));
                                                    break;
                                                case "UnitHydro":
                                                    conx = (new UnitHydrograph(fromComp.index, toComp.index, paramTable[i, 3], this));
                                                    break;
                                                case "TriggeredPWLinRes":
                                                    conx = (new TriggeredPWLinRes(fromComp.index, toComp.index, paramTable[i, 3]));
                                                   break;
                                                case "PwlGradientBasedFlow":
                                                    conx = (new PwlGradientBasedFlow(fromComp.index, toComp.index, paramTable[i, 3]));
                                                     break;
                                                default:
                                                    throw new NotImplementedException("Unknown connection type: " + paramTable[i, 2]);
                                            }
                                            
                                            if(toComp is FlowDivider)
                                            {
                                                conx.bToFlowDivider = true;
                                            }
                                            

                                            connections.Add(conx);

                                            i++;
                                        }
                                    }
                                }
                                else if (paramTable[i + 1, 1] == "splitter")
                                {
                                    FlowDivider div = getFlowDivider(paramTable[i, 1]);
                                    i = i + 2;
                                    while (paramTable[i, 0] == "<connection>")
                                    {
                                        Node toComp = getNode(paramTable[i, 1]);
                                        FlowDividerConnection divCon = new FlowDividerConnection(div.index, toComp.index, paramTable[i, 3]);
                                        divCon.flowDiv = div;
                                        div.connections.Add(divCon);

                                        connections.Add(divCon);
                                        i++;                                    
                                    }                                    
                                }
                            }
                        }
                    }
                }

                return connections;
            }
        }

        public bool checkForErrors()
        {
            bool bNoErrors = false;

            foreach (Connection con in connections)
            {
                if (con.GetType() == typeof(PieceWiseLinRes))
                {
                    for (int i = 0; i < ((PieceWiseLinRes)con).slopes.Length - 1; i++)
                    {
                        if (Double.IsNaN(((PieceWiseLinRes)con).slopes[i]))
                        {
                            throw new Exception("Error in slopes in PieceWiseLinRes: " + nodes[con.from].name + " " + nodes[con.to].name + " Migth be due to to identical volume data points. ");
                        }
                    }
                }
            }

            bNoErrors = true;
            return bNoErrors;
        }

        public Node getNode(string v)
        {
            foreach (var comp in nodes)
            {
                if (comp.name == v) return comp;
            }

            throw new Exception("No compartment called " + v);
        }


        public Node getNodeOrDivider(string v)
        {
            foreach (var comp in nodes)
            {
                if (comp.name == v) return comp;
            }
            foreach (var div in this.flowDividers)
            {
                if (div.name == v) return div;
            }

            throw new Exception("No node or divider called " + v);
        }

        private FlowDivider getFlowDivider(string v)
        {
            foreach (var div in flowDividers)
            {
                if (div.name == v) return div;
            }

            throw new Exception("No FlowDivider called " + v);
        }

        private static List<Node> instantiateNodes(string[,] paramTable, List<FlowDivider> flowDividers)
        {
            var Nodes = new List<Node>();
            bool bInHydraulicSection = false;

            for (int i = 0; i < paramTable.GetLength(0); i++)
            {
                if (paramTable[i, 0] == "[HydraulicModel]") bInHydraulicSection = true;

                if (bInHydraulicSection)
                {
                    if (paramTable[i, 0] == "[EndSect]") bInHydraulicSection = false;
                    else
                    {
                        //instantiate all compartments
                        if (paramTable[i, 0] == "<name>" && paramTable[i + 1, 0] == "<type>")
                        {
                            //if (paramTable[i + 1, 0] == "<type>" && paramTable[i + 1, 1] == "drainage")
                            switch (paramTable[i + 1, 1])
                            {
                                case DrainageCompartment.tag:
                                    Nodes.Add(new DrainageCompartment(paramTable[i, 1]));
                                    break;
                                case Surface1Compartment.tag:
                                    Nodes.Add(new Surface1Compartment(paramTable[i, 1]));
                                    break;
                                case Outlet.tag:
                                    Nodes.Add(new Outlet(paramTable[i, 1]));
                                    break;
                                case "splitter":
                                    flowDividers.Add(new FlowDivider(paramTable[i, 1]));
                                    break;
                                default:
                                    throw new Exception("Wrong type in prm");                                  
                            }
                        }
                    }
                }
            }

            return Nodes;
        }

        static public string[,] getParameterTableFromFile(string parameterFileFullPath)
        {
            const int MaxNumberOfColumnsInFile = 20; //

            //reading file content into array of strings. 
            var paramLines = new List<string>();
            using (var rd = new System.IO.StreamReader(parameterFileFullPath))
            {
                while (!rd.EndOfStream)
                {
                    paramLines.Add(rd.ReadLine());
                }
            }

            //parsing read lines into table of strings

            var paramTable = new String[paramLines.Count, MaxNumberOfColumnsInFile];

            int j = -1;
            for (int i = 0; i < paramLines.Count; i++)
            {
                bool bIsEmpty = true;

                string[] split = paramLines[i].Split(new Char[] { ' ', '\t', '=','+' }, StringSplitOptions.RemoveEmptyEntries);
                int column = 0;
                foreach (string s in split)
                {
                    if (s[0] == '/')
                    {
                        break; //If comment go to next line.
                    }
                    else
                    {
                        if (bIsEmpty) j++;
                        bIsEmpty = false;
                        paramTable[j, column] = s;
                        column++;
                    }
                }
            }

            return paramTable;
        }

        public void setOutFile(string filename)
        {
            this.logFile = new System.IO.StreamWriter(filename, true);
            logFile.WriteLine("first line");
        }

        public bool addWQOutputVariable(string fromNode, string toNode, string name)
        {
            addOutputVariable(fromNode, toNode, name);
            int n = output.dataCollection.Count;
            output.dataCollection[n-1].type = SmOutput.OutputType.WQmassFlux;
            return true;
        }

        public bool addOutputVariable(string fromNode, string toNode, string name)
        {
            bool bsuccess = false;
            Node N1 = getNodeOrDivider(fromNode);
            Node N2 = getNodeOrDivider(toNode);
            // string toName = getNode(toNode).name;
            //problem at outlets for samme index - lav om. 

            int fromIndex = N1.index;
            int toIndex = N2.index;
            bool bFromDivider = false;
            bool bToDivder = false;
            if (N1 is FlowDivider) bFromDivider = true;
            if (N2 is FlowDivider) bToDivder = true;

            foreach (Connection con in connections)
            {

                if (con.from == fromIndex && con.to == toIndex)
                {
                    //adapt to dividers
                    if (bFromDivider)
                    {
                        if( !(flowDividers[fromIndex].name == fromNode))
                        {
                            continue;
                        } 
                    }
                    if (bToDivder)
                    {
                        if (!(flowDividers[toIndex].name == toNode))
                        {
                            continue;
                        }
                    }
                    //end adapt to dividers
                    SmOutput xout = new SmOutput();
                    xout.name = name;
                    con.bIsOutput = true;
                    xout.con = con;
                    output.addNewDataSeries(xout);
                    bsuccess = true;
                }
            }

            return bsuccess;
        }


        public bool addOutputVariable(SmOutput.OutputType type)
        {
            bool bsuccess = false;

            SmOutput xout = new SmOutput();
            xout.type = type;
            xout.name = type.ToString();
            output.addNewDataSeries(xout);
            bsuccess = true;
            return bsuccess;
        }

        public bool addOutputVariable(string nodeName, int outputTypeNumber)
        { bool success = false;
            
            switch(outputTypeNumber)
            {
                    case (int)SmOutput.OutputType.nodeVolume:

                        success = addOutputVariable(nodeName, SmOutput.OutputType.nodeVolume );
                        break;
                default:
                    break;
            }
            return success;
        }

        public bool addOutputVariable(string nodeName, SmOutput.OutputType type)
        {
            bool bsuccess = false;

            if (type != SmOutput.OutputType.nodeVolume) throw new Exception("Unsupported output type for node " + nodeName);

            foreach (Node _node in nodes)
            {
                if (_node.name == nodeName)
                {
                    SmOutput xout = new SmOutput();
                    xout.type = SmOutput.OutputType.nodeVolume;
                    xout.name = "Volume in " + nodeName;
                    xout.nodex = _node;

                    output.addNewDataSeries(xout);
                    bsuccess = true;
                }
            }

            if (!bsuccess) throw new Exception("could not find output variable " + nodeName);

            return bsuccess;
        }

        public bool addOutputVariable(string outletName)
        {
            bool bsuccess = false;
            foreach (int i in iOutlets)
            {
                if (nodes[i].name == outletName)
                {
                    SmOutput xout = new SmOutput();
                    xout.type = SmOutput.OutputType.outletFlowTimeSeries;
                    xout.name = outletName;
                    xout.outletx = (Outlet)nodes[i];
                    output.addNewDataSeries(xout);
                    bsuccess = true;
                }
            }
            if (!bsuccess) throw new Exception("could not find output variable " + outletName);
            return bsuccess;
        }

        public void releaseOutFile()
        {
            if (logFile != null) logFile.Close();
        }

        public double[] getParameters()
        {
            if (logFile != null) logFile.WriteLine("getParameters()");
            List<double[]> allParamsList = new List<double[]>();

            foreach (Connection con in connections)
            {
                allParamsList.Add(con.getParameterArray());
            }

            foreach (Catchment cat in catchments)
            {
                allParamsList.Add(cat.getParameterArray());
            }


            int totalNumberOfDoubles = 0;
            foreach (var item in allParamsList)
            {
                totalNumberOfDoubles += item.Count();
            }

            double[] parameterArray = new double[totalNumberOfDoubles];
            int i = 0;
            foreach (var item in allParamsList)
            {
                item.CopyTo(parameterArray, i);
                i += item.Count();
            }

            if (logFile != null) logFile.WriteLine("getParameters() returning param array");
            return parameterArray;
        }

        public void writeCommentInOutFile()
        {
            if (logFile != null) logFile.WriteLine("Comment in");
        }

        public void saveModelParameters(string prmFileFullPath)
        {
            System.IO.StreamWriter prmFile;
            prmFile = new System.IO.StreamWriter(prmFileFullPath, false);
            prmFile.WriteLine("[HydraulicModel]");
            prmFile.WriteLine("\t************************************");
            foreach (Node x in nodes)
            {
                prmFile.WriteLine("\t<name>	" + x.name);
                prmFile.WriteLine("\t<type>   " + x.typeTag());
                foreach (Connection con in connections)
                {
                    if (nodes[con.from].name == x.name) //unneccesary strcomp  - index could do it?
                    {
                        if (con.to == System.Int32.MaxValue) //outlets are given MaxValue as magic number.
                        {
                            prmFile.WriteLine("\t<connection> " + "outlet" + "  " + con.typeTag() + " " + con.parameterString());
                        }
                        else
                        {
                            prmFile.WriteLine("\t<connection> " + nodes[con.to].name + "  " + con.typeTag() + " " + con.parameterString());
                        }
                    }
                }


                prmFile.WriteLine("\t************************************");
            }

            prmFile.WriteLine("[EndSect]");
            prmFile.WriteLine("/>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
            prmFile.WriteLine("/>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");


            prmFile.WriteLine("[Runoff]");
            prmFile.WriteLine("[SurfaceModels]");

            foreach (Catchment cat in catchments)
            {
                prmFile.WriteLine("\t<SurfMod>    " + cat.node.name + "  " + cat.typeTag() + " " + cat.parameterString());
            }


            prmFile.WriteLine("[EndSect]");
            prmFile.WriteLine("[EndSect]");


            prmFile.Close();
        }

        public List<Connection> Connections => connections;

        public List<Connection> getConnections()
        {
            return connections;
        }

        public List<string> CatchmentNodeNames => catchments.Select(c => c.node.name).ToList();

        public List<string> NodeNames => nodes.Select(n => n.name).ToList();

        public List<String> getCatchmentNodeNames()
        {
            List<String> cano = new List<string>(catchments.Count);
            foreach (var item in catchments)
            {
                cano.Add(item.node.name);
            }

            return cano;
        }

        public string getNodeName(int index)
        {
            return nodes[index].name;
        }

        public double[] getSurfaceModuleStates()
        {
            int nStates = 0;
            foreach (Catchment catx in catchments)
            {
                if (catx is LinResSurf2)
                {
                    nStates += 2; // ((LinResSurf2)catx).setRainfallData(raindata);
                }
                else
                {
                    throw new Exception("getSurfaceModuleStates not implementet for: " + catx.GetType());
                }
            }

            double[] surfState = new double[nStates];
            int i = 0;
            foreach (Catchment catx in catchments)
            {
                if (catx is LinResSurf2)
                {
                    surfState[i] = ((LinResSurf2)catx).state[0];
                    surfState[i + 1] = ((LinResSurf2)catx).state[1];
                    i += 2;
                }
            }

            return surfState;
        }

        public void setSurfaceModuleStates(double[] newStates)
        {
            foreach (Catchment catx in catchments)
            {
                int i = 0;
                if (catx is LinResSurf2)
                {
                    ((LinResSurf2)catx).state[0] = newStates[i];
                    ((LinResSurf2)catx).state[1] = newStates[i + 1];
                    i += 2; // 
                }
                else
                {
                    throw new Exception("setSurfaceModuleStates not implementet for: " + catx.GetType());
                }
            }
        }


        ~MainModel()
        {
            if (logFile != null) logFile.Close();
        }
    }
}