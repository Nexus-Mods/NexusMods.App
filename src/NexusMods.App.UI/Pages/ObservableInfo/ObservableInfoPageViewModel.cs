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
}

public class ObservableInfoPageViewModel : APageViewModel<IObservableInfoPageViewModel>, IObservableInfoPageViewModel
{
    private readonly HashSet<TrackingState> _trackingStatesBuffer = [];

    private readonly ObservableList<TrackingState> _trackingStates = [];
    public IReadOnlyList<TrackingState> TrackingStates { get; }

    private readonly ObservableDictionary<string, int> _topTypes = [];
    public IReadOnlyList<PieSeries<int>> Series { get; }

    public BindableReactiveProperty<Optional<TrackingState>> SelectedItem { get; } = new(value: Optional<TrackingState>.None);

    public ObservableInfoPageViewModel(IWindowManager windowManager) : base(windowManager)
    {
        TrackingStates = _trackingStates.ToNotifyCollectionChangedSlim();
        Series = _topTypes.ToNotifyCollectionChanged(static kv => new PieSeries<int>
        {
            Name = kv.Key,
            Values = [kv.Value],
        });

        this.WhenActivated(disposables =>
        {
            Observable
                .Timer(dueTime: TimeSpan.Zero, period: TimeSpan.FromSeconds(1), timeProvider: TimeProvider.System)
                .Do(this, static (_, self) =>
                {
                    self._trackingStatesBuffer.Clear();

                    ObservableTracker.CheckAndResetDirty();
                    ObservableTracker.ForEachActiveTask(trackingState =>
                    {
                        self._trackingStatesBuffer.Add(trackingState);
                    });

                    var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    foreach (var state in self._trackingStatesBuffer)
                    {
                        var count = counts.GetValueOrDefault(state.FormattedType, 0);
                        counts[state.FormattedType] = count + 1;
                    }

                    var top = counts
                        .OrderByDescending(static kv => kv.Value)
                        .Take(count: 10)
                        .Where(static kv => kv.Value > 1)
                        .ToDictionary();

                    var keysToRemove = self._topTypes
                        .Select(static kv => kv.Key)
                        .Except(top.Keys, StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    foreach (var key in keysToRemove)
                    {
                        self._topTypes.Remove(key);
                    }

                    foreach (var kv in top)
                    {
                        if (self._topTypes.TryGetValue(kv.Key, out var existingValue))
                        {
                            if (existingValue == kv.Value) continue;
                        }

                        self._topTypes[kv.Key] = kv.Value;
                    }
                })
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (_, self) =>
                {
                    var toRemove = self._trackingStates.Except(self._trackingStatesBuffer).ToArray();
                    var toAdd = self._trackingStatesBuffer.Except(self._trackingStates).ToArray();

                    foreach (var state in toRemove)
                    {
                        self._trackingStates.Remove(state);
                    }

                    self._trackingStates.AddRange(toAdd);
                })
                .AddTo(disposables);
        });
    }
}
