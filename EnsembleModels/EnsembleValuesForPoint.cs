using System;

using DtuSmModels;


namespace EnsembleModels
{
    public class EnsembleValuesForPoint
    {

        //private bool bTransform = false;//true if value should be transformed with some observation operator. 
        private readonly MainModel[] Models;
        public DerivedValue _derived;
        private readonly Outlet[] outlets;
        private readonly Connection[] cons;
        private readonly Node nodex;
        public double[] Values;
        private readonly SmOutput.OutputType _type;

        


        public EnsembleValuesForPoint(MainModel[] models, SmOutput.OutputType type, string name, string additionalParams)
        {
            Models = models;
            Values = new double[Models.Length];
            _type = type;
            switch (_type)
            {
                case SmOutput.OutputType.linkFlowTimeSeries:
                    throw new NotImplementedException();
                    for (int i = 0; i < Models.Length; i++)
                    {
                        //cons[i] = models[i].get
                    }
                    break;
                case SmOutput.OutputType.nodeVolume:
                    nodex = Models[0].getNode(name);
                    break;
                case SmOutput.OutputType.nodeWaterLevel:
                    nodex = Models[0].getNode(name);
                    _type = SmOutput.OutputType.nodeWaterLevel;
                    _derived = new DerivedValue(additionalParams);

                    break;
                case SmOutput.OutputType.outletFlowTimeSeries:

                    outlets = new Outlet[Models.Length];
                 
                    int outletIndexInNodeArray = int.MaxValue;
                    foreach (int i in Models[0].iOutlets)
                    {
                        if (Models[0].Nodes[i].name == name) outletIndexInNodeArray = i;

                    }
                    if (outletIndexInNodeArray == int.MaxValue) throw new Exception("could not find outlet");

                    for (int i = 0; i < Models.Length; i++)
                    {
                        outlets[i] = (Outlet)models[i].Nodes[outletIndexInNodeArray];
                    }
    
                    break;
                default:
                    break;
            }



        }


        public double[] GetValues()
        {
            switch (_type)
            {
                case SmOutput.OutputType.linkFlowTimeSeries:
                    throw new NotImplementedException();
                    //xout.updateData(xout.con.getFlow());
                    break;
                case SmOutput.OutputType.nodeWaterLevel:
                    //throw new NotImplementedException();
                    for (int i = 0; i < Values.Length; i++)
                    {
                        Values[i] = _derived.calculate(Models[i].state.values[nodex.index]);
            }
                    break;
                case SmOutput.OutputType.nodeVolume:
                    for (int i = 0; i < Values.Length; i++)
                    {
                        Values[i] = Models[i].state.values[nodex.index];
                    }
                    break;
                case SmOutput.OutputType.outletFlowTimeSeries:
                    for (int i = 0; i < Values.Length; i++)
                    {
                        Values[i] = outlets[i].flow;//MB: rewrite to avoid this unnecessary looping and copying. 
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            return Values;

        }






        




    }
}
 