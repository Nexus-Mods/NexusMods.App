using System.Collections.Concurrent;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Collections.Json;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.Networking.NexusWebApi;

namespace NexusMods.Collections;

using ModAndDownload = (Mod Mod, CollectionDownload.ReadOnly Download);

/// <summary>
/// Job for installing a collection.
/// </summary>
public class InstallCollectionJob : IJobDefinitionWithStart<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly>
{ 
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public required NexusModsCollectionLibraryFile.ReadOnly SourceCollection { get; init; }
    public required CollectionRevisionMetadata.ReadOnly RevisionMetadata { get; init; }
    public required CollectionDownload.ReadOnly[] Items { get; init; }
    public required Optional<NexusCollectionLoadoutGroup.ReadOnly> Group { get; init; }

    public required IServiceProvider ServiceProvider { get; init; }
    public required IFileStore FileStore { get; init; }
    public required ILibraryService LibraryService { get; init; }
    public required IConnection Connection { get; init; }
    public required LoadoutId TargetLoadout { get; init; }
    public required NexusModsLibrary NexusModsLibrary { get; init; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Factory.
    /// </summary>
    public static IJobTask<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly> Create(
        IServiceProvider provider,
        LoadoutId target,
        NexusModsCollectionLibraryFile.ReadOnly source,
        CollectionRevisionMetadata.ReadOnly revisionMetadata,
        CollectionDownload.ReadOnly[] items,
        Optional<NexusCollectionLoadoutGroup.ReadOnly> group)
    {
        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new InstallCollectionJob
        {
            Items = items,
            Group = group,
            TargetLoadout = target,
            SourceCollection = source,
            RevisionMetadata = revisionMetadata,
            ServiceProvider = provider,
            FileStore = provider.GetRequiredService<IFileStore>(),
            LibraryService = provider.GetRequiredService<ILibraryService>(),
            Connection = provider.GetRequiredService<IConnection>(),
            NexusModsLibrary = provider.GetRequiredService<NexusModsLibrary>(),
        };

        return monitor.Begin<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly>(job);
    }

    /// <summary>
    /// Factory.
    /// </summary>
    public static IJobTask<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly> Create(
        IServiceProvider provider,
        LoadoutId target,
        CollectionRevisionMetadata.ReadOnly revisionMetadata,
        CollectionDownload.ReadOnly[] items,
        Optional<NexusCollectionLoadoutGroup.ReadOnly> group)
    {
        var connection = provider.GetRequiredService<IConnection>();
        var datoms = connection.Db.Datoms(
            (NexusModsCollectionLibraryFile.CollectionSlug, revisionMetadata.Collection.Slug),
            (NexusModsCollectionLibraryFile.CollectionRevisionNumber, revisionMetadata.RevisionNumber)
        );

        if (datoms.Count == 0) throw new Exception($"Unable to find collection file for revision `{revisionMetadata.Collection.Slug}` (`{revisionMetadata.RevisionNumber}`)");
        var source = NexusModsCollectionLibraryFile.Load(connection.Db, datoms[0]);

        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new InstallCollectionJob
        {
            Items = items,
            Group = group,
            TargetLoadout = target,
            SourceCollection = source,
            RevisionMetadata = revisionMetadata,
            ServiceProvider = provider,
            FileStore = provider.GetRequiredService<IFileStore>(),
            Connection = connection,
            LibraryService = provider.GetRequiredService<ILibraryService>(),
            NexusModsLibrary = provider.GetRequiredService<NexusModsLibrary>(),
        };

        return monitor.Begin<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly>(job);
    }

    /// <summary>
    /// Installs the collection.
    /// </summary>
    public async ValueTask<NexusCollectionLoadoutGroup.ReadOnly> StartAsync(IJobContext<InstallCollectionJob> context)
    {
        var isReady = CollectionDownloader.IsFullyDownloaded(Items, db: Connection.Db);
        if (!isReady) throw new InvalidOperationException("The collection hasn't fully been downloaded!");

        var root = await NexusModsLibrary.ParseCollectionJsonFile(SourceCollection, context.CancellationToken);
        var modsAndDownloads = GatherDownloads(root);

        NexusCollectionLoadoutGroup.ReadOnly collectionGroup;
        if (Group.HasValue)
        {
            collectionGroup = Group.Value;
        }
        else
        {
            using var tx = Connection.BeginTransaction() ;
            var group = new NexusCollectionLoadoutGroup.New(tx, out var id)
            {
                CollectionId = RevisionMetadata.Collection,
                RevisionId = RevisionMetadata,
                LibraryFileId = SourceCollection,
                CollectionGroup = new CollectionGroup.New(tx, id)
                {
                    IsReadOnly = true,
                    LoadoutItemGroup = new LoadoutItemGroup.New(tx, id)
                    {
                        IsGroup = true,
                        LoadoutItem = new LoadoutItem.New(tx, id)
                        {
                            Name = root.Info.Name,
                            LoadoutId = TargetLoadout,
                        },
                    },
                },
            };

            var groupResult = await tx.Commit();
            collectionGroup = groupResult.Remap(group);
        }

        var installed = new ConcurrentBag<(ModAndDownload, LoadoutItemGroup.ReadOnly)>();
        await Parallel.ForEachAsync(modsAndDownloads, context.CancellationToken, async (modAndDownload, _) =>
        {
            var result = await InstallMod(modAndDownload, collectionGroup);
            installed.Add((modAndDownload, result));
        });

        using (var tx = Connection.BeginTransaction())
        {
            foreach (var (tuple, modGroup) in installed)
            {
                tx.Add(modGroup.Id, LoadoutItem.Name, tuple.Mod.Name);
            }

            await tx.Commit();
        }

        return collectionGroup;
    }

    private async ValueTask<LoadoutItemGroup.ReadOnly> InstallMod(ModAndDownload modAndDownload, NexusCollectionLoadoutGroup.ReadOnly collectionGroup)
    {
        var monitor = ServiceProvider.GetRequiredService<IJobMonitor>();

        var job = new InstallCollectionDownloadJob
        {
            Item = modAndDownload.Download,
            CollectionMod = modAndDownload.Mod,
            Group = collectionGroup.AsCollectionGroup(),
            TargetLoadout = TargetLoadout,
            SourceCollection = SourceCollection,

            ServiceProvider = ServiceProvider,
            Connection = Connection,
            FileStore = FileStore,
            LibraryService = LibraryService,
        };

        return await monitor.Begin<InstallCollectionDownloadJob, LoadoutItemGroup.ReadOnly>(job);
    }

    private List<ModAndDownload> GatherDownloads(CollectionRoot root)
    {
        var map = Items.ToDictionary(static download => download.ArrayIndex, static download => download);
        var list = new List<ModAndDownload>();

        foreach (var kv in map)
        {
            var (index, download) = kv;
            var mod = root.Mods[index];

            list.Add((mod, download));
        }

        return list;
    }
}
