using System.Reactive;
using Avalonia.Media;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPanelTabViewModel : IViewModelInterface
{
    public PanelTabId Id { get; }

    public PanelTabIndex Index { get; }

    public string Title { get; set; }

    public IImage? Icon { get; set; }

    public IViewModel? Contents { get; set; }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
}
