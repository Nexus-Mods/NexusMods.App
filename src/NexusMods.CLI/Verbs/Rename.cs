using NexusMods.Abstractions.CLI;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Rename a loadout id to a specific registry name
/// </summary>
public class Rename : AVerb<Loadout, string>
{
    private readonly LoadoutRegistry _registry;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="manager"></param>
    public Rename(LoadoutRegistry registry) => _registry = registry;

    /// <inheritdoc />
    public static VerbDefinition Definition => new("rename",
        "Rename a loadout id to a specific registry name", new OptionDefinition[]
        {
            new OptionDefinition<Loadout>("l", "loadout", "Loadout to assign a name"),
            new OptionDefinition<string>("n", "name", "Name to assign the loadout")
        });

    /// <inheritdoc />
    public Task<int> Run(Loadout loadout, string name, CancellationToken token)
    {
        _registry.Alter(loadout.LoadoutId, $"Renamed {loadout.DataStoreId} to {name}", _ => loadout);
        return Task.FromResult(0);
    }
}
