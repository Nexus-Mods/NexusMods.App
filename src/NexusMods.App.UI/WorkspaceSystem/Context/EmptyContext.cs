using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.App.UI.WorkspaceSystem;

[JsonName("NexusMods.App.UI.WorkspaceSystem.EmptyContext")]
public record EmptyContext : IWorkspaceContext
{
    public static readonly IWorkspaceContext Instance = new EmptyContext();
}
