namespace NexusMods.App.UI.WorkspaceSystem;

public record PageData
{
    public required PageFactoryId FactoryId { get; init; }

    public required IPageFactoryContext Context { get; init; }
}
