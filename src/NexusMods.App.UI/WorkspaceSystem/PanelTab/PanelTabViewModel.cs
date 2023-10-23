using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelTabViewModel : AViewModel<IPanelTabViewModel>, IPanelTabViewModel
{
    /// <inheritdoc/>
    public PanelTabId Id { get; } = PanelTabId.From(Guid.NewGuid());

    /// <inheritdoc/>
    public PanelTabIndex Index { get; set; }

    /// <inheritdoc/>
    public IPanelTabHeaderViewModel Header { get; private set; }

    /// <inheritdoc/>
    [Reactive] public IViewModel? Contents { get; set; }

    /// <inheritdoc/>
    [Reactive] public bool IsVisible { get; set; } = true;

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

    public TabData ToData()
    {
        return new TabData
        {
            Id = Id
        };
    }
}
