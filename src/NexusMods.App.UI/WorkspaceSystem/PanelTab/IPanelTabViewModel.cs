using System.Reactive;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPanelTabViewModel : IViewModelInterface
{
    /// <summary>
    /// Gets the unique identifier of the tab.
    /// </summary>
    public PanelTabId Id { get; }

    /// <summary>
    /// Gets the header view model of the tab.
    /// </summary>
    public IPanelTabHeaderViewModel Header { get; }

    /// <summary>
    /// Gets or sets the contents of tab.
    /// </summary>
    public Page Contents { get; set; }

    /// <summary>
    /// Gets or sets whether the tab contents is visible.
    /// </summary>
    public bool IsVisible { get; set; }

    public ReactiveCommand<Unit, Unit> GoBackInHistoryCommand { get; }

    public ReactiveCommand<Unit, Unit> GoForwardInHistoryCommand { get; }

    /// <summary>
    /// Transforms the current state of the tab into a serializable data format.
    /// </summary>
    public TabData? ToData();
}
