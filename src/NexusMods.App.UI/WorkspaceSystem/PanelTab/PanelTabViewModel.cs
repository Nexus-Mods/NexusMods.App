using System.Reactive.Disposables;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelTabViewModel : AViewModel<IPanelTabViewModel>, IPanelTabViewModel
{
    public PanelTabId Id { get; } = PanelTabId.From(Guid.NewGuid());

    public PanelTabIndex Index { get; set; }

    public IPanelTabHeaderViewModel Header { get; private set; }

    public IViewModel? Contents { get; set; }

    public PanelTabViewModel(IPanelViewModel panelViewModel, PanelTabIndex index)
    {
        Index = index;
        Header = new PanelTabHeaderViewModel(panelViewModel, Id);

        this.WhenActivated(disposables =>
        {
            Disposable.Create(this, state =>
            {
                if (state.Contents is IDisposable disposable) disposable.Dispose();
                state.Contents = null;
                state.Header = null!;
            }).DisposeWith(disposables);
        });
    }
}
