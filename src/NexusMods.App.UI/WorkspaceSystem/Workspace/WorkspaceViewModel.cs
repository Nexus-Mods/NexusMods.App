using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class WorkspaceViewModel : AViewModel<IWorkspaceViewModel>, IWorkspaceViewModel
{
    private readonly SourceCache<IPanelViewModel, PanelId> _panelSource = new(x => x.Id);

    private ReadOnlyObservableCollection<IPanelViewModel> _panels = Initializers.ReadOnlyObservableCollection<IPanelViewModel>();
    public ReadOnlyObservableCollection<IPanelViewModel> Panels => _panels;

    public ReactiveCommand<Unit, Unit> AddPanelCommand { get; private set; } = Initializers.DisabledReactiveCommand;
    public ReactiveCommand<Unit, Unit> RemovePanelCommand { get; private set; } = Initializers.DisabledReactiveCommand;

    public WorkspaceViewModel()
    {
        this.WhenActivated(disposables =>
        {
            _panelSource
                .Connect()
                .Sort(PanelComparer.Instance)
                .Bind(out _panels)
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);

            var canAddPanel = _panelSource.CountChanged.Select(count => count < 4);
            AddPanelCommand = ReactiveCommand.Create(() =>
            {
                _panelSource.AddOrUpdate(new PanelViewModel());
            }, canAddPanel).DisposeWith(disposables);

            var canRemovePanel = _panelSource.CountChanged.Select(count => count > 0);
            RemovePanelCommand = ReactiveCommand.Create(() =>
            {
                _panelSource.RemoveKey(_panelSource.Keys.First());
            }, canRemovePanel);
        });
    }
}
