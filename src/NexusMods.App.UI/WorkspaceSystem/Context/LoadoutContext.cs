using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.App.UI.WorkspaceSystem;

[JsonName("NexusMods.App.UI.WorkspaceSystem.LoadoutContext")]
public record LoadoutContext : IWorkspaceContext
{
    public required LoadoutId LoadoutId { get; init; }

    public bool IsValid(IServiceProvider serviceProvider)
    {
        var loadout = Loadout.Load(serviceProvider.GetRequiredService<IConnection>().Db, LoadoutId.Value);
        return loadout.IsValid();
    }
}
