namespace NexusMods.Abstractions.Jobs;

public class CompositeProgress : IProgressUpdater, IProgress<double>
{
    private readonly int _maxSteps;
    private int _currentStep;
    private bool _isFinished = false;
    private readonly Action<Percent> _progressCallback;
    private double _stepProgress;
    private DateTimeOffset _lastUpdate = DateTimeOffset.MinValue;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.25);

    public CompositeProgress(int steps, Action<Percent> progressCallback)
    {
        _maxSteps = steps;
        _currentStep = 0;
        _progressCallback = progressCallback;
    }
    
    public void NextStep()
    {
        if (_isFinished)
            return;
        _currentStep += 1;
        if (_currentStep >= _maxSteps) 
            _isFinished = true;
        UpdateProgress();
    }

    private void UpdateProgress()
    {
        try
        {
            if (DateTimeOffset.Now - _lastUpdate < UpdateInterval)
                return;
            
            _lastUpdate = DateTimeOffset.Now;
            var stepsProgress = _currentStep / (double)_maxSteps;
            var thisStepProgress = _stepProgress / _maxSteps;
            _progressCallback(new Percent(stepsProgress + thisStepProgress));
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public void SetStepProgress(double progress)
    {
        if (_isFinished)
            return;
        _stepProgress = progress;
        UpdateProgress();
    }

    public void Report(double value)
    {
        SetStepProgress(value);
    }
}
