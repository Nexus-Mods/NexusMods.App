using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
[JsonName("NexusMods.App.UI.WorkspaceSystem.WorkspaceData")]
public sealed record WorkspaceData : Entity
{
    public override EntityCategory Category => EntityCategory.Workspaces;

    public required PanelData[] Panels { get; init; }
}
