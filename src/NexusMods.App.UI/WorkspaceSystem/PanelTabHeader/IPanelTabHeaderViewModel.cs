using System.Reactive;
using NexusMods.App.UI.Controls.GenericIcon;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPanelTabHeaderViewModel : IViewModelInterface
{
    public PanelTabId Id { get; }

    public string Title { get; set; }

    public IconValue? Icon { get; set; }

    public bool IsSelected { get; set;  }

    public bool CanClose { get; set; }

    public ReactiveCommand<Unit, PanelTabId> CloseTabCommand { get; }
}
