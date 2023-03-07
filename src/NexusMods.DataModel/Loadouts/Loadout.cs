using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Loadouts;

[JsonName("NexusMods.DataModel.Loadout")]
public record Loadout : Entity, IEmptyWithDataStore<Loadout>
{
    public required EntityDictionary<ModId, Mod> Mods { get; init; }
    public required LoadoutId LoadoutId { get; init; }

    public required string Name { get; init; }
    public required GameInstallation Installation { get; init; }
    public required DateTime LastModified { get; init; }
    public required EntityLink<Loadout> PreviousVersion { get; init; }

    public override EntityCategory Category => EntityCategory.Loadouts;
    public required string ChangeMessage { get; init; } = "";

    /// <inheritdoc />
    public static Loadout Empty(IDataStore store) => new()
    {
        LoadoutId = LoadoutId.Create(),
        Installation = GameInstallation.Empty,
        Name = "",
        Mods = EntityDictionary<ModId, Mod>.Empty(store),
        LastModified = DateTime.UtcNow,
        PreviousVersion = EntityLink<Loadout>.Empty(store),
        ChangeMessage = "",
        Store = store
    };

    public Loadout Alter(ModId modId, Func<Mod, Mod?> func)
    {
        return this with
        {
            Mods = Mods.Keep(modId, func)
        };
    }

    public Loadout Add(Mod mod)
    {
        return this with
        {
            Mods = Mods.With(mod.Id, mod)
        };
    }

    public Loadout AlterFiles(Func<AModFile, AModFile?> func)
    {
        return this with
        {
            Mods = Mods.Keep(m => m with
            {
                Files = m.Files.Keep(func)
            })
        };
    }
}
