using JetBrains.Annotations;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
public sealed record TabData
{
    public required PanelTabId Id { get; init; }

    public required APageData PageData { get; init; }
}
