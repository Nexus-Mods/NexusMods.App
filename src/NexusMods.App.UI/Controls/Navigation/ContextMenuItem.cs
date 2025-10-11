using System.Reactive;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Navigation;

/// <summary>
/// Default implementation of <see cref="IContextMenuItem"/>.
/// </summary>
public class ContextMenuItem : IContextMenuItem
{
    /// <inheritdoc/>
    public string Header { get; init; } = string.Empty;
    
    /// <inheritdoc/>
    public IconValue? Icon { get; init; }
    
    /// <inheritdoc/>
    public ReactiveCommand<Unit, Unit> Command { get; init; } = ReactiveCommand.Create(() => { });
    
    /// <inheritdoc/>
    public bool IsVisible { get; init; } = true;
    
    /// <inheritdoc/>
    public ContextMenuItemStyling Styling { get; init; } = ContextMenuItemStyling.Default;

}
