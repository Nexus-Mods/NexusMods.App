using System.Reactive.Subjects;
using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

[PublicAPI]
public abstract class AProgress : IMutableProgress
{
    public Optional<DateTime> FirstStartTime { get; private set; } = Optional<DateTime>.None;
    public Optional<DateTime> LastStartTime { get; private set; } = Optional<DateTime>.None;
    public Optional<DateTime> FinishTime { get; private set; } = Optional<DateTime>.None;
    public TimeSpan TotalDuration { get; private set; } = TimeSpan.Zero;

    private readonly Subject<TimeSpan> _subjectTotalDuration = new();
    public IObservable<TimeSpan> ObservableTotalDuration => _subjectTotalDuration;

    public void SetFirstStartTime(DateTime value)
    {
        // TODO: sanity checks
        FirstStartTime = value;
    }

    public void SetLastStartTime(DateTime value)
    {
        // TODO: sanity checks
        LastStartTime = value;
    }

    public void SetFinishTime(DateTime value)
    {
        // TODO: sanity checks
        FinishTime = value;
    }

    public void IncreaseTotalDuration(TimeSpan amount)
    {
        // TODO: sanity checks
        TotalDuration += amount;
        _subjectTotalDuration.OnNext(TotalDuration);
    }
}
