using System.Reactive;
using BitFaster.Caching.Lru;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Models;
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
        return serviceCollection.AddKeyedSingleton<IResourceLoader<UnrealEnginePakLoadoutFile.ReadOnly, Outcome<Dictionary<string, PakMetaData>>>>(
            serviceKey: MetadataPipelineKey,
            implementationFactory: static (serviceProvider, _) => CreateMetadataPipeline(
                fileStore: serviceProvider.GetRequiredService<IFileStore>(),
                connection: serviceProvider.GetRequiredService<IConnection>(),
                gameRegistry: serviceProvider.GetRequiredService<IGameRegistry>(),
                temporaryFileManager: serviceProvider.GetRequiredService<TemporaryFileManager>()
            )
        );
    }
    
    public static IResourceLoader<UnrealEnginePakLoadoutFile.ReadOnly, Outcome<Dictionary<string, PakMetaData>>> GetMetadataPipeline(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<IResourceLoader<UnrealEnginePakLoadoutFile.ReadOnly, 
            Outcome<Dictionary<string, PakMetaData>>>>(serviceKey: MetadataPipelineKey);
    }
    
    private static IResourceLoader<UnrealEnginePakLoadoutFile.ReadOnly, Outcome<Dictionary<string, PakMetaData>>> CreateMetadataPipeline(
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
                    if (!Utils.TryGetUnrealEngineGameAddon(gameRegistry, loadout.Installation.GameId, out var ueGameAddon))
                        throw new InvalidDataException("Could not find UE game addon.");
                    
                    var pakFile = loadout.Items
                        .OfTypeLoadoutItemGroup()
                        .SelectMany(group => group.Children.OfTypeLoadoutItemWithTargetPath()
                            .OfTypeLoadoutFile())
                        .FirstOrDefault(file => file.Hash == hash).ToUnrealEnginePakLoadoutFile();
                    if (!LibraryArchive.TryGet(connection.Db, pakFile.LibraryArchiveId, out var archive)) 
                        throw new InvalidDataException("Could not find library archive.");
                    
                    var pakMetadata = await PakFileParser.ExtractAndDeserialize(
                        ueGameAddon!,
                        temporaryFileManager,
                        fileStore,
                        archive.Value,
                        token);
                    return resource.WithData(Outcome.FromResult(pakMetadata));
                }
                catch (InvalidDataException e)
                {
                    return resource.WithData(Outcome.FromException<Dictionary<string, PakMetaData>>(e));
                }
            })
            .UseCache(
                keyGenerator: static hash => hash,
                keyComparer: EqualityComparer<Hash>.Default,
                capacityPartition: new FavorWarmPartition(totalCapacity: 100)
            )
            .ChangeIdentifier<UnrealEnginePakLoadoutFile.ReadOnly, Hash, Outcome<Dictionary<string, PakMetaData>>>(
                static mod => mod.AsLoadoutFile().Hash
            );
    
        return pipeline;
    }
}
