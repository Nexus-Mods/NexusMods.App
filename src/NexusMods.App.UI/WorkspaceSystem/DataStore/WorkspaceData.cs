using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
public record WorkspaceData : Entity
{
    public override EntityCategory Category => EntityCategory.Workspaces;

    public required PanelData[] Panels { get; init; }
}
