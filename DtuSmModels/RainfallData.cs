namespace DtuSmModels
{
    internal class RainfallData
    {
        internal double[] data;
 
        public RainfallData() { }

        public void setRainData(double[] raindata)
        {
            data = raindata;
            
        }

        public double getRain(int index)
        {
            if (index < 0) {
                return 0;
                    }
            else
            {
        return data[index];
            }         
        }
    }
}