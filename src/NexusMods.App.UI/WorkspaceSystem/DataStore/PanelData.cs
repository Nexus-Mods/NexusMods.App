using Avalonia;
using JetBrains.Annotations;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
public record PanelData
{
    public required PanelId Id { get; init; }

    public required Rect LogicalBounds { get; init; }

    public required TabData[] Tabs { get; init; }
}
