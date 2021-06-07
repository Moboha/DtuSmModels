namespace DtuSmModels
{
    public abstract class Node
    {   
        public string name { get; }
        public int index; 
        public bool bHasVolume;

        protected Node(string name)
        {
            this.name = name;
            bHasVolume = true;//false for dividers.
        }

        abstract public string typeTag();
    }
}