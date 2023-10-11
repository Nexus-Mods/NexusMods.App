using System.Reactive;
using Avalonia.Media;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelTabHeaderDesignViewModel : AViewModel<IPanelTabHeaderViewModel>, IPanelTabHeaderViewModel
{
    public PanelTabId Id { get; } = PanelTabId.From(Guid.Empty);

    public string Title { get; set; } = "My Mods";

    public IImage? Icon { get; set; } = Initializers.IImage;

    [Reactive]
    public bool IsSelected { get; set; }

    public ReactiveCommand<Unit, Unit> CloseTabCommand => Initializers.EnabledReactiveCommand;
}
