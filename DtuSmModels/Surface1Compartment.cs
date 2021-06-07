using System;

namespace DtuSmModels
{
    internal class Surface1Compartment : Compartment
    {
        public const string tag = "surface1";
        public Surface1Compartment(string name) : base(name) { }

        public override string typeTag()
        {
            return tag;
        }
    }
}