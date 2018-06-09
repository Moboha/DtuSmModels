using System;

namespace DtuSmModels
{
    // internal class Outlet : Node
    public class Outlet : Compartment
    {
        public static readonly string tag = "outlet";
        public Outlet(string name) : base(name) { }
        //      this.index = System.Int32.MaxValue;
        //  }
        public double flow;


        public override string typeTag()
        {
            return tag;
        }
    }
}