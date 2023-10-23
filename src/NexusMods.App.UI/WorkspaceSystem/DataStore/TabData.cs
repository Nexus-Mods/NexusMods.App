using JetBrains.Annotations;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
public record TabData
{
    public required PanelTabId Id { get; init; }
}
