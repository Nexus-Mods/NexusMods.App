using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
public sealed record WorkspaceData
{
    public required PanelData[] Panels { get; init; }
}
