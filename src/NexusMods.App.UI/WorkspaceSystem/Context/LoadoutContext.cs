using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.App.UI.WorkspaceSystem;

[JsonName("NexusMods.App.UI.WorkspaceSystem.LoadoutContext")]
public record LoadoutContext : IWorkspaceContext
{
    public required LoadoutId LoadoutId { get; init; }

    public bool IsValid(IServiceProvider serviceProvider)
    {
        var loadoutRegistry = serviceProvider.GetRequiredService<ILoadoutRegistry>();
        return loadoutRegistry.Contains(LoadoutId);
    }
}
