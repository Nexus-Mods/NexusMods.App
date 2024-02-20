using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
[JsonName("NexusMods.App.UI.WorkspaceSystem.WindowData")]
public sealed record WindowData : Entity
{
    public static IId Id => new Id64(EntityCategory.Workspaces, 0);

    public override EntityCategory Category => EntityCategory.Workspaces;

    public required WorkspaceId? ActiveWorkspaceId { get; init; }

    public required WorkspaceData[] Workspaces { get; init; }
}
