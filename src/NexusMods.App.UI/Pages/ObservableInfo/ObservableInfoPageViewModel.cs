using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ObservableCollections;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ObservableInfo;

public interface IObservableInfoPageViewModel : IPageViewModelInterface
{
    NotifyCollectionChangedSynchronizedViewList<TrackingState> TrackingStates { get; }
}

public class ObservableInfoPageViewModel : APageViewModel<IObservableInfoPageViewModel>, IObservableInfoPageViewModel
{
    private readonly List<TrackingState> _trackingStatesBuffer = [];

    private readonly ObservableList<TrackingState> _trackingStates = [];
    public NotifyCollectionChangedSynchronizedViewList<TrackingState> TrackingStates { get; }

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

                    self._trackingStatesBuffer.Sort();
                })
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (_, self) =>
                {
                    self._trackingStates.Clear();
                    self._trackingStates.AddRange(self._trackingStatesBuffer);
                })
                .AddTo(disposables);
        });
    }
}
