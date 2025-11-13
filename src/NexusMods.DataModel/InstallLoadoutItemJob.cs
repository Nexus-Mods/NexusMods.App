using System.Diagnostics;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Games.AdvancedInstaller;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.Loadouts;
using NexusMods.Sdk.Tracking;
using NexusMods.Sdk.Library;

namespace NexusMods.DataModel;

internal class InstallLoadoutItemJob : IJobDefinitionWithStart<InstallLoadoutItemJob, InstallLoadoutItemJobResult>, IInstallLoadoutItemJob
{
    public required ILogger Logger { get; init; }
    public ILibraryItemInstaller? Installer { get; init; }
    public ILibraryItemInstaller? FallbackInstaller { get; init; }
    public LibraryItem.ReadOnly LibraryItem { get; init; }
    public Optional<LoadoutItemGroupId> ParentGroupId { get; set; }
    public LoadoutId LoadoutId { get; init; }
    
    public required ITransaction Transaction { get; init; }
    internal required IConnection Connection { get; init; }
    internal required IServiceProvider ServiceProvider { get; init; }

    /// <remarks>
    /// Returns null <see cref="LoadoutItemGroup.ReadOnly"/> after running job
    /// if supplied an external transaction via <paramref name="transaction"/>.
    ///
    /// (i.e. if you are running this job as part of a larger transaction)
    /// </remarks>
    public static InstallLoadoutItemJob Create(
        IServiceProvider serviceProvider,
        LibraryItem.ReadOnly libraryItem,
        LoadoutId targetLoadout,
        ITransaction transaction,
        Optional<LoadoutItemGroupId> groupId = default,
        ILibraryItemInstaller? installer = null,
        ILibraryItemInstaller? fallbackInstaller = null)
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();
        var job = new InstallLoadoutItemJob
        {
            Logger = serviceProvider.GetRequiredService<ILogger<InstallLoadoutItemJob>>(),
            Installer = installer,
            FallbackInstaller = fallbackInstaller,
            LibraryItem = libraryItem,
            LoadoutId = targetLoadout,
            ParentGroupId = groupId,
            Connection = connection,
            ServiceProvider = serviceProvider,
            Transaction = transaction,
        };

        return job;
    }

    public async ValueTask<InstallLoadoutItemJobResult> StartAsync(IJobContext<InstallLoadoutItemJob> context)
    {
        if (!ParentGroupId.HasValue)
        {
            var collection = Loadout.Load(Connection.Db, LoadoutId).MutableCollections().First().CollectionId;
            ParentGroupId = LoadoutItemGroupId.From(collection);
        }

        await context.YieldAsync();
        
        var loadout = Loadout.Load(Connection.Db, LoadoutId);

        var installers = Installer is not null
            ? [Installer]
            : loadout.InstallationInstance.GetGame().LibraryItemInstallers;

        var sw = Stopwatch.StartNew();
        var result = await ExecuteInstallersAsync(installers, loadout, context);

        if (result == null)
        {
            if (Installer is AdvancedManualInstaller)
            {
                if (TryExtract(out var fileId, out var modId, out var gameId, out var modUid, out var fileUid))
                    Events.ModsInstallationFailed(fileId, modId, gameId, modUid, fileUid);
                throw new InvalidOperationException($"Advanced installer did not succeed for `{LibraryItem.Name}` (`{LibraryItem.Id}`)");
            }

            var fallbackInstaller = FallbackInstaller ?? AdvancedManualInstaller.Create(ServiceProvider);
            result = await ExecuteInstallersAsync([fallbackInstaller], loadout, context);

            if (result == null)
            {
                if (TryExtract(out var fileId, out var modId, out var gameId, out var modUid, out var fileUid))
                    Events.ModsInstallationFailed(fileId, modId, gameId, modUid, fileUid);
                throw new InvalidOperationException($"Found no installer that supports `{LibraryItem.Name}` (`{LibraryItem.Id}`), including the fallback installer!");
            }
        }

        {
            if (TryExtract(out var fileId, out var modId, out var gameId, out var modUid, out var fileUid))
                Events.ModsInstallationCompleted(fileId, modId, gameId, modUid, fileUid, sw);
        }

        // TODO(erri120): rename this entity to something unique, like "LoadoutItemInstalledFromLibrary"
        var loadoutGroup = result!;
        _ = new LibraryLinkedLoadoutItem.New(Transaction, loadoutGroup.Id)
        {
            LoadoutItemGroup = loadoutGroup,
            LibraryItemId = LibraryItem,
        };

        return new InstallLoadoutItemJobResult(null, loadoutGroup);
    }

    private async ValueTask<LoadoutItemGroup.New?> ExecuteInstallersAsync(
        ILibraryItemInstaller[] installers,
        Loadout.ReadOnly loadout,
        IJobContext<InstallLoadoutItemJob> context)
    {
        foreach (var installer in installers)
        {
            var isSupported = installer.IsSupportedLibraryItem(LibraryItem);
            if (!isSupported) continue;

            using var subTransaction = context.Definition.Transaction.CreateSubTransaction();
            
            var loadoutGroup = new LoadoutItemGroup.New(subTransaction, out var groupId)
            {
                IsGroup = true,
                LoadoutItem = new LoadoutItem.New(subTransaction, groupId)
                {
                    Name = LibraryItem.Name,
                    LoadoutId = LoadoutId,
                    ParentId = ParentGroupId.Value,
                },
            };

            // TODO(erri120): add safeguards to only allow groups to be added to the parent groups
            try
            {
                var result = await installer.ExecuteAsync(LibraryItem, loadoutGroup, subTransaction, loadout, context.CancellationToken);
                if (result.IsNotSupported(out var reason))
                {
                    if (Logger.IsEnabled(LogLevel.Trace) && !string.IsNullOrEmpty(reason))
                        Logger.LogTrace("Installer doesn't support library item `{LibraryItem}` because \"{Reason}\"", LibraryItem.Name, reason);
                    continue;
                }

                Debug.Assert(result.IsSuccess);
                subTransaction.CommitToParent();
                return loadoutGroup;
            }
            catch (OperationCanceledException ex)
            {
                if (TryExtract(out var fileId, out var modId, out var gameId, out var modUid, out var fileUid))
                    Events.ModsInstallationCancelled(fileId, modId, gameId, modUid, fileUid);

                context.CancelAndThrow(ex.Message);
            }
        }

        return null;
    }

    private bool TryExtract(out uint fileId, out uint modId, out uint gameId, out ulong modUid, out ulong fileUid)
    {
        fileId = 0;
        modId = 0;
        gameId = 0;
        modUid = 0;
        fileUid = 0;
        if (!LibraryItem.TryGetAsNexusModsLibraryItem(out var nexusModsLibraryItem)) return false;

        fileId = nexusModsLibraryItem.FileMetadata.Uid.FileId.Value;
        modId = nexusModsLibraryItem.FileMetadata.ModPage.Uid.ModId.Value;
        gameId = nexusModsLibraryItem.FileMetadata.Uid.GameId.Value;
        modUid = nexusModsLibraryItem.FileMetadata.ModPage.Uid.AsUlong;
        fileUid = nexusModsLibraryItem.FileMetadata.Uid.AsUlong;
        return true;
    }
}
