namespace NexusMods.App.UI.WorkspaceSystem;

public record PageDiscoveryDetails
{
    public required string SectionName { get; init; }

    public required string ItemName { get; init; }

    public required PageData PageData { get; init; }
}
