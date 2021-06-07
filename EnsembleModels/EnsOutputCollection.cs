using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EnsembleModels
{
    public class EnsOutputCollection
    {
        public Boolean bGotTime { get; }
        public List<double> timeInSeconds; //always start at zero.
        public List<EnsOutput> hydraulicOutput;

        public EnsOutputCollection()
        {
            hydraulicOutput = new List<EnsOutput>();
            timeInSeconds = new List<double>();
        }

        public void addNewDataSeries(EnsOutput eOut)
        {
            hydraulicOutput.Add(eOut);
            timeInSeconds.Clear();

        }

        public EnsOutput[] getData()
        {
            return hydraulicOutput.ToArray();
        }

        public void resetOutputSeries()
        {
            timeInSeconds.Clear();
            foreach (EnsOutput smo in hydraulicOutput)
            {
                foreach( DtuSmModels.SmOutput mo in smo.dataSeries)
                {                 
                mo.clear();
                }
            }
        }
    }


}
