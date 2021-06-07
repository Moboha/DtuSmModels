namespace EnsembleModels
{
    public interface IEnsembleNoise
    {
        //Returns the next values from the realization for all members of the ensemble
        double[] DrawNext();

        //Returns the next values from the realizations for all members for the next N time steps.
        // returns double[ensembleMember][timestep]
        double[][] DrawNext(int Nsteps);


        double[] DrawNextValuesForMember(int member, int Nsteps);

    }
}