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
        /*
            Note(Sewer)
            
            In App, we create a loadout workspace for each loadout.

            It may be possible that this loadout no longer exists, as a result
            of removal from the CLI (UI not running) or a crash during removal.
            [i.e. in cases we may have not run explicit workspace delete]
            
            In such cases, we may need to discard invalid loadouts, which we
            do here.
        */
        var loadoutRegistry = serviceProvider.GetRequiredService<ILoadoutRegistry>();
        return loadoutRegistry.Contains(LoadoutId);
    }
}
