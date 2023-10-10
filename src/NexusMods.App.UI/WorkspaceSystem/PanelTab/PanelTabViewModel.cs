using System.Reactive;
using Avalonia.Media;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelTabViewModel : AViewModel<IPanelTabViewModel>, IPanelTabViewModel
{
    public PanelTabId Id { get; } = PanelTabId.From(Guid.NewGuid());

    public PanelTabIndex Index { get; }

    public string Title { get; set; } = "New Tab";

    public IImage? Icon { get; set; }

    public IViewModel? Contents { get; set; }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; } = Initializers.DisabledReactiveCommand;

    public PanelTabViewModel(PanelTabIndex index)
    {
        Index = index;
    }
}
