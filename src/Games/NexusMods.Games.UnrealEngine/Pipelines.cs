using System.Reactive;
using BitFaster.Caching.Lru;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Resources.Caching;
using NexusMods.Abstractions.Resources.IO;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.Games.UnrealEngine.Models;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using Polly;

namespace NexusMods.Games.UnrealEngine;

public static class Pipelines
{
    public const string MetadataPipelineKey = nameof(MetadataPipelineKey);

    public static IServiceCollection AddPipelines(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddKeyedSingleton<IResourceLoader<UnrealEnginePakLoadoutFile.ReadOnly, Outcome<PakMetaData>>>(
            serviceKey: MetadataPipelineKey,
            implementationFactory: static (serviceProvider, _) => CreateMetadataPipeline(
                fileStore: serviceProvider.GetRequiredService<IFileStore>(),
                connection: serviceProvider.GetRequiredService<IConnection>(),
                gameRegistry: serviceProvider.GetRequiredService<IGameRegistry>(),
                temporaryFileManager: serviceProvider.GetRequiredService<TemporaryFileManager>()
            )
        );
    }
    
    public static IResourceLoader<UnrealEnginePakLoadoutFile.ReadOnly, Outcome<PakMetaData>> GetMetadataPipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<UnrealEnginePakLoadoutFile.ReadOnly, 
            Outcome<PakMetaData>>>(serviceKey: MetadataPipelineKey);
    }
    
    private static IResourceLoader<UnrealEnginePakLoadoutFile.ReadOnly, Outcome<PakMetaData>> CreateMetadataPipeline(
        IFileStore fileStore,
        IConnection connection,
        IGameRegistry gameRegistry,
        TemporaryFileManager temporaryFileManager)
    {
        var pipeline = new FileStoreStreamLoader(fileStore)
            .ThenDo(Unit.Default, async (_, hash, resource, token) =>
            {
                try
                {
                    var loadout = Loadout.All(connection.Db)
                        .FirstOrDefault(loadout => loadout.Items
                            .OfTypeLoadoutItemGroup()
                            .SelectMany(group => group.Children.OfTypeLoadoutItemWithTargetPath()
                                .OfTypeLoadoutFile())
                            .Select(file => file.Hash)
                            .Contains(hash)
                        );
                    if (!loadout.IsValid()) throw new InvalidDataException("Could not find valid loadout for file hash.");
                    var ueGameAddon = gameRegistry.InstalledGames
                        .Where(game => game.Game.GameId == loadout.Installation.GameId)
                        .Select(game => game.GetGame())
                        .Cast<IUnrealEngineGameAddon>()
                        .FirstOrDefault();
                    if (ueGameAddon == null)
                        throw new InvalidDataException("Could not find UE game addon.");
                    await using (var tempFile = temporaryFileManager.CreateFile(Constants.PakExt))
                    {
                        await using var fs = tempFile.Path.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                        await resource.Data.CopyToAsync(fs, token);
                        var metaData = await PakFileParser.ParsePakMeta(ueGameAddon, tempFile.Path.ToString());
                        return resource.WithData(Outcome.FromResult(metaData));
                    }
                }
                catch (InvalidDataException e)
                {
                    return resource.WithData(Outcome.FromException<PakMetaData>(e));
                }
            })
            .UseCache(
                keyGenerator: static hash => hash,
                keyComparer: EqualityComparer<Hash>.Default,
                capacityPartition: new FavorWarmPartition(totalCapacity: 100)
            )
            .ChangeIdentifier<UnrealEnginePakLoadoutFile.ReadOnly, Hash, Outcome<PakMetaData>>(
                static mod => mod.AsLoadoutFile().Hash
            );
    
        return pipeline;
    }
}
