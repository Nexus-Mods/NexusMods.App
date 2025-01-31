using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.AdvancedInstaller;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Library;

internal class InstallLoadoutItemJob : IJobDefinitionWithStart<InstallLoadoutItemJob, InstallLoadoutItemJobResult>, IInstallLoadoutItemJob
{
    public required ILogger Logger { get; init; }
    public ILibraryItemInstaller? Installer { get; init; }
    public ILibraryItemInstaller? FallbackInstaller { get; init; }
    public LibraryItem.ReadOnly LibraryItem { get; init; }
    public LoadoutItemGroupId ParentGroupId { get; init; }
    public LoadoutId LoadoutId { get; init; }
    public required ITransaction Transaction { get; init; }
    required internal IConnection Connection { get; init; }
    required internal IServiceProvider ServiceProvider { get; init; }
    required internal bool OwnsTransaction { get; init; }

    /// <remarks>
    /// Returns null <see cref="LoadoutItemGroup.ReadOnly"/> after running job
    /// if supplied an external transaction via <paramref name="transaction"/>.
    ///
    /// (i.e. if you are running this job as part of a larger transaction)
    /// </remarks>
    public static IJobTask<InstallLoadoutItemJob, InstallLoadoutItemJobResult> Create(
        IServiceProvider serviceProvider,
        LibraryItem.ReadOnly libraryItem,
        LoadoutItemGroupId groupId,
        ILibraryItemInstaller? installer = null,
        ILibraryItemInstaller? fallbackInstaller = null, 
        ITransaction? transaction = null)
    {
        var group = LoadoutItemGroup.Load(libraryItem.Db, groupId);
        var connection = serviceProvider.GetRequiredService<IConnection>();
        var job = new InstallLoadoutItemJob
        {
            Logger = serviceProvider.GetRequiredService<ILogger<InstallLoadoutItemJob>>(),
            Installer = installer,
            FallbackInstaller = fallbackInstaller,
            LibraryItem = libraryItem,
            ParentGroupId = groupId,
            LoadoutId = group.AsLoadoutItem().LoadoutId,
            Connection = connection,
            ServiceProvider = serviceProvider,
            Transaction = transaction ?? connection.BeginTransaction(),
            OwnsTransaction = transaction is null,
        };
        return serviceProvider.GetRequiredService<IJobMonitor>().Begin<InstallLoadoutItemJob, InstallLoadoutItemJobResult>(job);
    }

    public async ValueTask<InstallLoadoutItemJobResult> StartAsync(IJobContext<InstallLoadoutItemJob> context)
    {
        await context.YieldAsync();
        
        var loadout = Loadout.Load(Connection.Db, LoadoutId);

        var installers = Installer is not null
            ? [Installer]
            : loadout.InstallationInstance.GetGame().LibraryItemInstallers;

        var result = await ExecuteInstallersAsync(installers, loadout, context);

        if (result == null)
        {
            if (Installer is AdvancedManualInstaller)
                throw new InvalidOperationException($"Advanced installer did not succeed for `{LibraryItem.Name}` (`{LibraryItem.Id}`)");

            var fallbackInstaller = FallbackInstaller ?? AdvancedManualInstaller.Create(ServiceProvider);
            result = await ExecuteInstallersAsync([fallbackInstaller], loadout, context);

            if (result == null)
                throw new InvalidOperationException($"Found no installer that supports `{LibraryItem.Name}` (`{LibraryItem.Id}`), including the fallback installer!");
        }

        // TODO(erri120): rename this entity to something unique, like "LoadoutItemInstalledFromLibrary"
        var loadoutGroup = result!;
        _ = new LibraryLinkedLoadoutItem.New(Transaction, loadoutGroup.Id)
        {
            LoadoutItemGroup = loadoutGroup,
            LibraryItemId = LibraryItem,
        };
        
        if (OwnsTransaction)
        {
            var transactionResult = await Transaction.Commit();
            Transaction.Dispose();
            return new InstallLoadoutItemJobResult(transactionResult.Remap(loadoutGroup));
        }
        
        // Part of an external transaction, so we return null.
        return new InstallLoadoutItemJobResult(null);
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

            var transaction = context.Definition.Transaction;
            var loadoutGroup = new LoadoutItemGroup.New(transaction, out var groupId)
            {
                IsGroup = true,
                LoadoutItem = new LoadoutItem.New(transaction, groupId)
                {
                    Name = LibraryItem.Name,
                    LoadoutId = LoadoutId,
                    ParentId = ParentGroupId,
                },
            };

            // TODO(erri120): add safeguards to only allow groups to be added to the parent groups
            var result = await installer.ExecuteAsync(LibraryItem, loadoutGroup, transaction, loadout, context.CancellationToken);
            if (result.IsNotSupported(out var reason))
            {
                if (Logger.IsEnabled(LogLevel.Trace) && !string.IsNullOrEmpty(reason))
                    Logger.LogTrace("Installer doesn't support library item `{LibraryItem}` because \"{Reason}\"", LibraryItem.Name, reason);

                continue;
            }

            Debug.Assert(result.IsSuccess);
            return loadoutGroup;
        }

        return null;
    }
}
