using System.Reactive.Subjects;
using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

[PublicAPI]
public class DeterminateProgress : AProgress, IDeterminateProgress
{
    public Percent Percent { get; private set; } = Percent.Zero;
    public ProgressRate ProgressRate { get; private set; }
    public Optional<DateTime> EstimatedFinishTime { get; private set; } = Optional<DateTime>.None;

    private readonly Subject<Percent> _subjectPercent = new();
    public IObservable<Percent> ObservablePercent => _subjectPercent;

    private readonly Subject<ProgressRate> _subjectProgressRate = new();
    public IObservable<ProgressRate> ObservableProgressRate => _subjectProgressRate;

    private readonly Subject<DateTime> _subjectEstimatedFinishTime = new();
    public IObservable<DateTime> ObservableEstimatedFinishTime => _subjectEstimatedFinishTime;

    public DeterminateProgress(IProgressRateFormatter formatter)
    {
        ProgressRate = new ProgressRate(value: 0.0, formatter);
    }

    internal void SetPercent(Percent value)
    {
        // TODO: sanity checks
        Percent = value;
        _subjectPercent.OnNext(value);
    }

    internal void SetProgressRate(ProgressRate value)
    {
        // TODO: sanity checks
        ProgressRate = value;
        _subjectProgressRate.OnNext(value);
    }

    internal void SetEstimatedFinishTime(DateTime value)
    {
        // TODO: sanity checks
        EstimatedFinishTime = value;
        _subjectEstimatedFinishTime.OnNext(value);
    }
}
