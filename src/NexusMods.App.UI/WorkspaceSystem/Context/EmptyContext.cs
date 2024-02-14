namespace NexusMods.App.UI.WorkspaceSystem;

public record EmptyContext : IWorkspaceContext
{
    public static readonly IWorkspaceContext Instance = new EmptyContext();
}
