using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
public sealed record WindowData
{
    public required WorkspaceId? ActiveWorkspaceId { get; init; }

    public required WorkspaceData[] Workspaces { get; init; }
}
