using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.WorkspaceSystem;

[JsonName("NexusMods.App.UI.WorkspaceSystem.LoadoutContext")]
public record LoadoutContext : IWorkspaceContext
{
    public required LoadoutId LoadoutId { get; init; }

    public bool IsValid(IServiceProvider serviceProvider)
    {
        var repo = serviceProvider.GetRequiredService<IRepository<Loadout.Model>>();
        return repo.Exists(LoadoutId.Value);
    }
}
