using System.Collections.Frozen;
using System.Collections.Immutable;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Backend.Games.Locators;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
[Obsolete("this is a hack that will be removed soon tm")]
internal class ManuallyAddedLocator : IGameLocator
{
    private readonly IConnection _connection;
    private readonly IFileSystem _fileSystem;
    private readonly FrozenDictionary<NexusModsGameId, IGameData> _registeredGames;

    public ManuallyAddedLocator(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

        _registeredGames = serviceProvider
            .GetServices<IGameData>()
            .Where(x => x.NexusModsGameId.HasValue)
            .Select(x => new KeyValuePair<NexusModsGameId, IGameData>(x.NexusModsGameId.Value, x))
            .ToFrozenDictionary();
    }

    public IEnumerable<GameLocatorResult> Locate()
    {
        var entities = ManuallyAddedGame.All(_connection.Db);
        foreach (var entity in entities)
        {
            if (!_registeredGames.TryGetValue(entity.GameId, out var game)) continue;

            yield return new GameLocatorResult
            {
                StoreIdentifier = entity.Id.ToString(),
                Path = _fileSystem.FromUnsanitizedFullPath(entity.Path),
                LocatorIds = ImmutableArray<LocatorId>.Empty,
                Game = game,
                Store = GameStore.ManuallyAdded,
                Locator = this,
            };
        }
    }
}
