namespace DtuSmModels
{
    abstract internal class Catchment
    {
        public Node node;
        protected RainfallData rain;
        protected int index;

        public abstract double getNextFlowInM3PrS();
        public abstract string typeTag();

        public abstract double[] getParameterArray();
        public abstract void setParameters(double[] specificParameters); //
        public abstract int setParameterArray(double[] newParameters, int i); //i is startindex in parameterarray for entire model. returns the number of parameters. 

        internal abstract string parameterString();

        internal void setRainfallData(RainfallData raindata)
        {
            this.rain = raindata;
            this.index = 0;
        }
    }
}