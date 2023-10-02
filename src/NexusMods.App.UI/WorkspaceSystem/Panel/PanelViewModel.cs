using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelViewModel : AViewModel<IPanelViewModel>, IPanelViewModel
{
    public PanelId Id { get; } = PanelId.From(Guid.NewGuid());

    [Reactive]
    public IViewModel? Content { get; set; }
}
