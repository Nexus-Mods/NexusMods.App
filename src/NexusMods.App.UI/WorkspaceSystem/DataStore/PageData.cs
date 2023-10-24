namespace NexusMods.App.UI.WorkspaceSystem;

public abstract record APageData
{
    public required PageFactoryId FactoryId { get; init; }
}
