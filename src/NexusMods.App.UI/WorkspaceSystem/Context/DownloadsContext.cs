using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.App.UI.WorkspaceSystem;

[JsonName("NexusMods.App.UI.WorkspaceSystem.DownloadsContext")]
public record DownloadsContext : IWorkspaceContext
{
    public bool IsValid(IServiceProvider serviceProvider) => true;
}
