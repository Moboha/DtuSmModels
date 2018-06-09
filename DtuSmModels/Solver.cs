namespace DtuSmModels
{
    public abstract class Solver
    {
        protected MainModel model;

        public Solver(MainModel model)
        {
            this.model = model;

        }

        public abstract double[] solve(double dt, double[] volumes, double[] forcing);
    }
}