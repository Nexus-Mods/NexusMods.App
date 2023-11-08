using Avalonia;
using JetBrains.Annotations;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
public sealed record PanelData
{
    public required Rect LogicalBounds { get; init; }

    public required TabData[] Tabs { get; init; }

    public required PanelTabId SelectedTabId { get; init; }
}
