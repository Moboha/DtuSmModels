using System.Collections.Generic;

namespace DtuSmModels
{
    public interface IMainModel
    {   
        //Setting up and initializing 
        void initializeFromFile(string parameterFileFullPath);
        void saveModelParameters(string prmFileFullPath);   
        void setParameter(double[] newParameters);
        bool checkForErrors();  

        //Set model forcing
        bool setIndividualRainData(string catchmentNode, double[] oneMinuteRainfallx);
        void setInitialCond(double[] init);
        void setRainDataForAllCatchments(double[] rainInMmOneMinSteps);   
             
        //output specification
        bool addOutputVariable(string outletName);
        bool addOutputVariable(SmOutput.outputType type);
        bool addOutputVariable(string fromNode, string toNode, string name);
        void setOutFile(string filename);
        void releaseOutFile();
        void writeCommentInOutFile();

        //getters
        List<string> getCatchmentNodeNames();
        List<Connection> getConnections();
        string getNodeName(int index);
        double[] getParameters();
        
        //Run model
        void modelStep(double dt, double[] forcing);
        void runForOneMinuteRainInput();
        void stepModelWithSetRain(int NumberOfSteps);      
          

    }
}