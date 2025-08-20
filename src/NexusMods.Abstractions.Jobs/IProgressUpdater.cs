namespace NexusMods.Abstractions.Jobs;

public interface IProgressUpdater : IProgress<double>
{
    public void NextStep();
    public void SetStepProgress(double progress);
    
    public void SetStepProgress(int current, int total) => SetStepProgress((double)current / total);
}
