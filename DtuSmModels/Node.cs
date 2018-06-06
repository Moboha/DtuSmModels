namespace DtuSmModels
{
    public abstract class Node
    {   
        public string name { get; }
        public int index; 

        protected Node(string name)
        {
            this.name = name;
        }

        abstract public string typeTag();
    }
}