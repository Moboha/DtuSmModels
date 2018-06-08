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
    {
        public static double TimeStepInSeconds { get; } = 60;
        public List<Node> Nodes => nodes;

        private List<Node> nodes;
        private List<Connection> connections;
        private List<Catchment> catchments;
        private int[] iOutlets; //index of outlets in the state vector.

        private double[] dydt; //change to state vector

        //public double[] volumes { get; set; } //state vector
        public int lengthOfStateVector;
        public double t; //in seconds
        private RainfallData raindata;
        private List<RainfallData> individualRainDatas;
        private int lenghtOfRainfallData;
        public StateVector state;


        private System.IO.StreamWriter logFile; //affald - slet igen senere. 
        public SmOutputCollection output;
        private Solver mySolver;

        public MainModel()
        {
            //Console.Write("New Surrogate model");
            Compartment.resetNumberOfCompartments();
            raindata = new RainfallData();
            this.output = new SmOutputCollection();
        }

        public void modelStep(double dt, double[] forcing)
        {
            state.values = mySolver.solve(dt, state.values, forcing);
            foreach (int i in iOutlets)
            {
                ((Outlet) nodes[i]).flow = state.values[i] / myconst.DT;
                state.values[i] = 0;
            }

            t += dt;
        }

        public void setInitialCond(double[] init)
        {
            state.values = init;
        }

        internal double[] calculateDyDt(double[] vols, double[] forcing)
        {
            //System.Array.Clear(dydt,0,lengthOfStateVector);
            double[] tempDyDt = new double[lengthOfStateVector];

            for (int i = 0; i < vols.Length; i++) //MB: Constraining to volumes above or equal to zero.
            {
                if (vols[i] < 0) vols[i] = 0;
            }


            foreach (Connection con in connections)
            {
                try
                {
                    int j = con.from;
                    double x = con.calculateFlow(vols);
                    tempDyDt[j] -= x;
                    if (con.to != System.Int32.MaxValue) //outlets are given MaxValue as magic number.
                    {
                        tempDyDt[con.to] += x;
                    }
                }
                catch (Exception ex1)
                {
                    Exception ex2 = new Exception("Time t=: " + t + " Error calculating flow: " + nodes[con.from].name + " to " + nodes[con.to].name + "   vol in from node=" + vols[con.from], ex1);
                    throw ex2;
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
                if (xout.type == SmOutput.outputType.GlobalVolumen)
                {
                    xout.updateData(state.values.Sum());
                }
                else if (xout.type == SmOutput.outputType.linkFlowTimeSeries)
                {
                    //  xout.updateData(xout.con.calculateFlow(state.values));
                    xout.updateData(xout.con.getFlow());
                }
                else if (xout.type == SmOutput.outputType.outletFlowTimeSeries)
                {
                    xout.updateData(xout.outletx.flow);
                }
                else if (xout.type == SmOutput.outputType.nodeVolume)
                {
                    xout.updateData(state.values[xout.nodex.index]);
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

        public void stepModelWithSetRain(int numberOfSteps)
        {
            for (int i = 0; i < numberOfSteps; i++)
            {
                double[] forcingVector = new double[state.values.Length];
                foreach (Catchment cat in catchments)
                {
                    forcingVector[((Compartment) cat.node).index] = cat.getNextFlowInM3PrS();
                }

                modelStep(myconst.DT, forcingVector);
                collectOutputData();
            }
        }

        public void initializeFromFile(string parameterFileFullPath)
        {
            string[,] paramTable = getParameterTableFromFile(parameterFileFullPath);
            this.nodes = instantiateNodes(paramTable);
            this.connections = getConnections(paramTable);
            this.catchments = getCatchments(paramTable);
            lengthOfStateVector = ((Compartment) nodes[0]).totalNumberOfCompartments();

            this.state = new StateVector();
            this.state.values = new double[lengthOfStateVector];
            this.dydt = new double[lengthOfStateVector];
            // Connection.state = this.state;
            var xioutlets = new List<int>();
            foreach (Node n in nodes)
            {
                if (n is Outlet) xioutlets.Add(n.index);
            }

            this.iOutlets = xioutlets.ToArray();


            this.mySolver = new RungeKutta4(this);

            if (logFile != null)
            {
                logFile.WriteLine(parameterFileFullPath);
                logFile.WriteLine("nodes: " + nodes.Count());
                logFile.WriteLine("connections: " + connections.Count());
                logFile.WriteLine("catchments: " + catchments.Count());
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

                        while (paramTable[i, 0] == "<SurfMod>")
                        {
                            switch (paramTable[i, 2])
                            {
                                case "TA1":
                                    TA1 taCatch = new TA1(getNode(paramTable[i, 1]), paramTable[i, 3]);
                                    taCatch.setRainfallData(raindata);
                                    xcatchments.Add(taCatch);
                                    break;
                                case "LinResSurf2":
                                    LinResSurf2 LrSurf = new LinResSurf2(getNode(paramTable[i, 1]), paramTable[i, 3]);
                                    LrSurf.setRainfallData(raindata);
                                    xcatchments.Add(LrSurf);
                                    break;
                                case "PlainArea":
                                    PlainArea plAr = new PlainArea(getNode(paramTable[i, 1]), paramTable[i, 3]);
                                    plAr.setRainfallData(raindata);
                                    xcatchments.Add(plAr);
                                    break;
                                default:
                                    throw new Exception("Error constructing surface model. Unknown SurfModel type: " + paramTable[i, 2]);
                            }

                            i++;
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
                            //instantiate all compartments
                            if (paramTable[i, 0] == "<name>")
                            {
                                {
                                    Node fromComp = getNode(paramTable[i, 1]);

                                    i = i + 2;
                                    while (paramTable[i, 0] == "<connection>")
                                    {
                                        Node toComp = getNode(paramTable[i, 1]);
                                        switch (paramTable[i, 2])
                                        {
                                            case "LinRes":
                                                connections.Add(new LinRes(fromComp.index, toComp.index, paramTable[i, 3]));
                                                break;
                                            case "LinResWithMax":
                                                connections.Add(new LinResWithMax(fromComp.index, toComp.index, paramTable[i, 3]));
                                                break;
                                            case "PieceWiseLinRes":
                                                connections.Add(new PieceWiseLinRes(fromComp.index, toComp.index, paramTable[i, 3]));
                                                break;
                                            case "LinResWithMaxAndBackWater":
                                                connections.Add(new LinResWithMaxAndBackWater(fromComp.index, toComp.index, paramTable[i, 3]));
                                                break;
                                            case "SpillingVolume":
                                                connections.Add(new SpillingVolume(fromComp.index, toComp.index, paramTable[i, 3]));
                                                break;
                                            case "UnitHydro":
                                                connections.Add(new UnitHydrograph(fromComp.index, toComp.index, paramTable[i, 3], this));
                                                break;
                                            case "TriggeredPWLinRes":
                                                connections.Add(new TriggeredPWLinRes(fromComp.index, toComp.index, paramTable[i, 3]));
                                                break;
                                            default:
                                                throw new NotImplementedException("Unknown connection type: " + paramTable[i, 2]);
                                        }

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
                    for (int i = 0; i < ((PieceWiseLinRes) con).slopes.Length - 1; i++)
                    {
                        if (Double.IsNaN(((PieceWiseLinRes) con).slopes[i]))
                        {
                            throw new Exception("Error in slopes in PieceWiseLinRes: " + nodes[con.from].name + " " + nodes[con.to].name + " Migth be due to to identical volume data points. ");
                        }
                    }
                }
            }

            bNoErrors = true;
            return bNoErrors;
        }

        private Node getNode(string v)
        {
            foreach (var comp in nodes)
            {
                if (comp.name == v) return comp;
            }

            throw new Exception("No compartment called " + v);
        }

        private static List<Node> instantiateNodes(string[,] paramTable)
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
                        if (paramTable[i, 0] == "<name>")
                        {
                            //if (paramTable[i + 1, 0] == "<type>" && paramTable[i + 1, 1] == "drainage")
                            if (paramTable[i + 1, 0] == "<type>" && paramTable[i + 1, 1] == DrainageCompartment.tag)
                            {
                                Nodes.Add(new DrainageCompartment(paramTable[i, 1]));
                            }

                            if (paramTable[i + 1, 0] == "<type>" && paramTable[i + 1, 1] == Surface1Compartment.tag)
                            {
                                Nodes.Add(new Surface1Compartment(paramTable[i, 1]));
                            }

                            if (paramTable[i + 1, 0] == "<type>" && paramTable[i + 1, 1] == Outlet.tag)
                            {
                                Nodes.Add(new Outlet(paramTable[i, 1]));
                            }
                        }
                    }
                }
            }

            return Nodes;
        }

        private string[,] getParameterTableFromFile(string parameterFileFullPath)
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

                string[] split = paramLines[i].Split(new Char[] {' ', '\t', '='}, StringSplitOptions.RemoveEmptyEntries);
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

        public bool addOutputVariable(string fromNode, string toNode, string name)
        {
            bool bsuccess = false;

            int fromIndex = getNode(fromNode).index;
            int toIndex = getNode(toNode).index;
            // string toName = getNode(toNode).name;
            //problem at outlets for samme index - lav om. 
            foreach (Connection con in connections)
            {
                if (con.from == fromIndex && con.to == toIndex)
                {
                    SmOutput xout = new SmOutput();
                    xout.name = name;
                    xout.con = con;
                    output.addNewDataSeries(xout);
                    bsuccess = true;
                }
            }

            return bsuccess;
        }

        public bool addOutputVariable(SmOutput.outputType type)
        {
            bool bsuccess = false;

            SmOutput xout = new SmOutput();
            xout.type = type;
            xout.name = type.ToString();
            output.addNewDataSeries(xout);
            bsuccess = true;
            return bsuccess;
        }

        public bool addOutputVariable(string nodeName, SmOutput.outputType type)
        {
            bool bsuccess = false;

            if (type != SmOutput.outputType.nodeVolume) throw new Exception("Unsupported output type for node " + nodeName);

            foreach (Node _node in nodes)
            {
                if (_node.name == nodeName)
                {
                    SmOutput xout = new SmOutput();
                    xout.type = SmOutput.outputType.nodeVolume;
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
                    xout.type = SmOutput.outputType.outletFlowTimeSeries;
                    xout.name = outletName;
                    //jeg er i gang her MB og alle andre steder relateret til output. :
                    foreach (Node _node in nodes)
                    {
                        if (_node.name == outletName) //not robust - add check for type.
                        {
                            xout.outletx = (Outlet) _node;
                        }
                    }

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
                    surfState[i] = ((LinResSurf2) catx).state[0];
                    surfState[i + 1] = ((LinResSurf2) catx).state[1];
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
                    ((LinResSurf2) catx).state[0] = newStates[i];
                    ((LinResSurf2) catx).state[1] = newStates[i + 1];
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