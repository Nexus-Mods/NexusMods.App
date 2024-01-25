using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
[JsonName("NexusMods.App.UI.WorkspaceSystem.WorkspaceData")]
public sealed record WorkspaceData : Entity
{
    public override EntityCategory Category => EntityCategory.Workspaces;

    public required PanelData[] Panels { get; init; }
}
