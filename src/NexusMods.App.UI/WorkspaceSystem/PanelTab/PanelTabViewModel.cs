using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using NexusMods.App.UI.Resources;
using ReactiveUI;
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

    public TabData ToData()
    {
        return new TabData
        {
            Id = Id,
            PageData = Contents.PageData
        };
    }

    public void SetTitle(string title)
    {
        Header.Title = title;
    }

    public void SetIcon(IImage? icon)
    {
        Header.Icon = icon;
    }
}
