using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtuSmModels
{
    public class SmOutputCollection
    {
        public Boolean bGotTime{get;}
        public List<double> timeInSeconds; //always start at zero.
        public List<SmOutput> dataCollection;

        public SmOutputCollection()
        {
            dataCollection = new List<SmOutput>();
            timeInSeconds = new List<double>();
        } 

        public void addNewDataSeries(SmOutput outx)
        {
            dataCollection.Add(outx);
            timeInSeconds.Clear();

        }

        public SmOutput[] getData()
        {
            return dataCollection.ToArray();
        }

        public void resetOutputSeries()
        {
            timeInSeconds.Clear();
            foreach(SmOutput smo in dataCollection)
            {
                smo.clear();
            }
        }
    }
}
