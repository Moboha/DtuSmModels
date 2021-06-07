using System;

namespace DtuSmModels
{
    public class DrainageCompartment : Compartment
    {
        public const string tag = "drainage";
        public DrainageCompartment(string name) : base(name) { }

        public override string typeTag()
        {
            return tag;
        }

    }
}