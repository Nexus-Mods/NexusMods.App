using DynamicData.Kernel;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ObservableCollections;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ObservableInfo;

public interface IObservableInfoPageViewModel : IPageViewModelInterface
{
    IReadOnlyList<TrackingState> TrackingStates { get; }

    BindableReactiveProperty<Optional<TrackingState>> SelectedItem { get; }
}

public class ObservableInfoPageViewModel : APageViewModel<IObservableInfoPageViewModel>, IObservableInfoPageViewModel
{
    private readonly HashSet<TrackingState> _trackingStatesBuffer = [];
    private readonly ObservableList<TrackingState> _trackingStates = [];

    public IReadOnlyList<TrackingState> TrackingStates { get; }
    public BindableReactiveProperty<Optional<TrackingState>> SelectedItem { get; } = new(value: Optional<TrackingState>.None);

    public ObservableInfoPageViewModel(IWindowManager windowManager) : base(windowManager)
    {
        TrackingStates = _trackingStates.ToNotifyCollectionChangedSlim();

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
