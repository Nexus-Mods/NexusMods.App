using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.UI.Sdk.Icons;
using NexusMods.App.UI.Resources;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelTabHeaderViewModel : AViewModel<IPanelTabHeaderViewModel>, IPanelTabHeaderViewModel
{
    public PanelTabId Id { get; }

    [Reactive]
    public string Title { get; set; } = Language.PanelTabHeaderViewModel_Title_New_Tab;

    [Reactive] public IconValue Icon { get; set; } = new();

    [Reactive] public bool CanClose { get; set; }

    [Reactive] public bool IsSelected { get; set; } = true;

    public ReactiveCommand<Unit, PanelTabId> CloseTabCommand { get; }

    public PanelTabHeaderViewModel(PanelTabId id)
    {
        Id = id;
        CloseTabCommand = ReactiveCommand.Create(() => Id, this.WhenAnyValue(vm => vm.CanClose));
    }
}
