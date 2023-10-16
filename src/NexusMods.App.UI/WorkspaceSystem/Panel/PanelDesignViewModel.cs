using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelDesignViewModel : AViewModel<IPanelViewModel>, IPanelViewModel
{
    public PanelId Id => PanelId.Empty;

    public ReactiveCommand<Unit, Unit> AddTabCommand => Initializers.DisabledReactiveCommand;
    public ReactiveCommand<Unit, Unit> CloseCommand => Initializers.DisabledReactiveCommand;

    public ReadOnlyObservableCollection<IPanelTabViewModel> Tabs => Initializers.ReadOnlyObservableCollection<IPanelTabViewModel>();
    public ReadOnlyObservableCollection<IPanelTabHeaderViewModel> TabHeaders => Initializers.ReadOnlyObservableCollection<IPanelTabHeaderViewModel>();

    public PanelTabId SelectedTabId { get; set; }

    public Rect LogicalBounds { get; set; } = new(0, 0, 1, 1);
    public Rect ActualBounds { get; } = new(0, 0, 1000, 400);

    public void Arrange(Size workspaceSize) => throw new NotSupportedException();
    public void CloseTab(PanelTabId id) => throw new NotSupportedException();
    public IPanelTabViewModel AddTab() => throw new NotSupportedException();
}
