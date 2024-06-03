using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelTabViewModel : AViewModel<IPanelTabViewModel>, IPanelTabViewModel
{
    /// <inheritdoc/>
    public PanelTabId Id { get; } = PanelTabId.From(Guid.NewGuid());

    /// <inheritdoc/>
    public IPanelTabHeaderViewModel Header { get; }

    /// <inheritdoc/>
    [Reactive] public required Page Contents { get; set; }

    /// <inheritdoc/>
    [Reactive] public bool IsVisible { get; set; } = true;

    public PanelTabViewModel()
    {
        Header = new PanelTabHeaderViewModel(Id);
    }

    public TabData? ToData()
    {
        if (Contents.PageData.Context.IsEphemeral) return null;

        return new TabData
        {
            Id = Id,
            PageData = Contents.PageData,
        };
    }
}
