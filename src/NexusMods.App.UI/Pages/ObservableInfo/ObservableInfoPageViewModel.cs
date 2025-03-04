using DynamicData.Kernel;
using LiveChartsCore.SkiaSharpView;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ObservableCollections;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ObservableInfo;

public interface IObservableInfoPageViewModel : IPageViewModelInterface
{
    IReadOnlyList<PieSeries<int>> Series { get; }

    IReadOnlyList<TrackingState> TrackingStates { get; }

    BindableReactiveProperty<Optional<TrackingState>> SelectedItem { get; }

    bool TrackStackTraces { get; }
}

public class ObservableInfoPageViewModel : APageViewModel<IObservableInfoPageViewModel>, IObservableInfoPageViewModel
{
    private readonly ObservableList<TrackingState> _trackingStates = [];
    public IReadOnlyList<TrackingState> TrackingStates { get; }

    private readonly ObservableDictionary<string, PieSeries<int>> _series = [];
    public IReadOnlyList<PieSeries<int>> Series { get; }

    public BindableReactiveProperty<Optional<TrackingState>> SelectedItem { get; } = new(value: Optional<TrackingState>.None);

    public bool TrackStackTraces { get; }

    public ObservableInfoPageViewModel(IWindowManager windowManager, bool trackStackTraces) : base(windowManager)
    {
        TrackStackTraces = trackStackTraces;

        TrackingStates = _trackingStates.ToNotifyCollectionChangedSlim();
        Series = _series.ToNotifyCollectionChanged(static kv => kv.Value);

        this.WhenActivated(disposables =>
        {
            ObservableTracker.EnableTracking = true;
            ObservableTracker.EnableStackTrace = trackStackTraces;

            Disposable.Create(() =>
            {
                ObservableTracker.EnableTracking = false;
                ObservableTracker.EnableStackTrace = false;
            }).AddTo(disposables);

            var statesToAdd = new HashSet<TrackingState>();
            var statesToRemove = new HashSet<TrackingState>();
            var countsToAdd = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var countsToRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Observable
                .Timer(dueTime: TimeSpan.Zero, period: TimeSpan.FromSeconds(1), timeProvider: TimeProvider.System)
                .Synchronize()
                .Select((this, statesToAdd, statesToRemove, countsToAdd, countsToRemove), static (_, tuple) =>
                {
                    var (self, statesToAdd, statesToRemove, countsToAdd, countsToRemove) = tuple;
                    statesToAdd.Clear();
                    statesToRemove.Clear();
                    countsToAdd.Clear();
                    countsToRemove.Clear();

                    ObservableTracker.CheckAndResetDirty();
                    ObservableTracker.ForEachActiveTask(trackingState =>
                    {
                        statesToAdd.Add(trackingState);

                        var count = countsToAdd.GetValueOrDefault(trackingState.FormattedType, 0);
                        countsToAdd[trackingState.FormattedType] = count + 1;
                    });

                    foreach (var trackingState in self._trackingStates)
                    {
                        if (statesToAdd.Remove(trackingState)) continue;
                        statesToRemove.Add(trackingState);
                    }

                    foreach (var kv in self._series)
                    {
                        if (countsToAdd.ContainsKey(kv.Key)) continue;
                        countsToRemove.Add(kv.Key);
                    }

                    var averageCount = (int)Math.Floor(countsToAdd.Values.Average());
                    return (statesToAdd, statesToRemove, countsToAdd, countsToRemove, averageCount);
                })
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (tuple, self) =>
                {
                    var (statesToAdd, statesToRemove, countsToAdd, countsToRemove, averageCount) = tuple;

                    self._trackingStates.AddRange(statesToAdd);
                    foreach (var trackingState in statesToRemove)
                    {
                        self._trackingStates.Remove(trackingState);
                    }

                    foreach (var kv in countsToAdd)
                    {
                        var invalidValue = kv.Value <= 1 || kv.Value < averageCount;

                        if (self._series.TryGetValue(kv.Key, out var value))
                        {
                            if (invalidValue)
                            {
                                countsToRemove.Add(kv.Key);
                                continue;
                            }

                            value.Values = [kv.Value];
                        }
                        else
                        {
                            if (invalidValue) continue;

                            self._series.Add(kv.Key, new PieSeries<int>
                            {
                                Name = kv.Key,
                                Values = [kv.Value],
                            });
                        }
                    }

                    foreach (var key in countsToRemove)
                    {
                        self._series.Remove(key: key);
                    }
                })
                .AddTo(disposables);
        });
    }
}
