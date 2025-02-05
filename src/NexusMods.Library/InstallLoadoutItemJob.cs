using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.AdvancedInstaller;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Library;

internal class InstallLoadoutItemJob : IJobDefinitionWithStart<InstallLoadoutItemJob, LoadoutItemGroup.ReadOnly>, IInstallLoadoutItemJob
{
    public ILibraryItemInstaller? Installer { get; init; }
    public ILibraryItemInstaller? FallbackInstaller { get; init; }
    public LibraryItem.ReadOnly LibraryItem { get; init; }
    public LoadoutItemGroupId ParentGroupId { get; init; }
    public LoadoutId LoadoutId { get; init; }
    internal required IConnection Connection { get; init; }
    internal required IServiceProvider ServiceProvider { get; init; }

    public static IJobTask<InstallLoadoutItemJob, LoadoutItemGroup.ReadOnly> Create(
        IServiceProvider serviceProvider,
        LibraryItem.ReadOnly libraryItem,
        LoadoutItemGroupId groupId,
        ILibraryItemInstaller? installer = null,
        ILibraryItemInstaller? fallbackInstaller = null)
    {
        var group = LoadoutItemGroup.Load(libraryItem.Db, groupId);
        var job = new InstallLoadoutItemJob
        {
            Installer = installer,
            FallbackInstaller = fallbackInstaller,
            LibraryItem = libraryItem,
            ParentGroupId = groupId,
            LoadoutId = group.AsLoadoutItem().LoadoutId,
            Connection = serviceProvider.GetRequiredService<IConnection>(),
            ServiceProvider = serviceProvider,
        };
        return serviceProvider.GetRequiredService<IJobMonitor>().Begin<InstallLoadoutItemJob, LoadoutItemGroup.ReadOnly>(job);
    }

    public async ValueTask<LoadoutItemGroup.ReadOnly> StartAsync(IJobContext<InstallLoadoutItemJob> context)
    {
        await context.YieldAsync();
        
        var loadout = Loadout.Load(Connection.Db, LoadoutId);

        var installers = Installer is not null
            ? [Installer]
            : loadout.InstallationInstance.GetGame().LibraryItemInstallers;

        var result = await ExecuteInstallersAsync(installers, loadout, context);

        if (!result.HasValue)
        {
            if (Installer is AdvancedManualInstaller)
                throw new InvalidOperationException($"Advanced installer did not succeed for `{LibraryItem.Name}` (`{LibraryItem.Id}`)");

            var fallbackInstaller = FallbackInstaller ?? AdvancedManualInstaller.Create(ServiceProvider);
            result = await ExecuteInstallersAsync([fallbackInstaller], loadout, context);

            if (!result.HasValue)
            {
                throw new InvalidOperationException($"Found no installer that supports `{LibraryItem.Name}` (`{LibraryItem.Id}`), including the fallback installer!");
            }
        }

        var (loadoutGroup, transaction) = result.Value;
        using var tx = transaction;

        // TODO(erri120): rename this entity to something unique, like "LoadoutItemInstalledFromLibrary"
        _ = new LibraryLinkedLoadoutItem.New(tx, loadoutGroup.Id)
        {
            LoadoutItemGroup = loadoutGroup,
            LibraryItemId = LibraryItem,
        };

        var transactionResult = await transaction.Commit();
        return transactionResult.Remap(loadoutGroup);
    }

    private async ValueTask<(LoadoutItemGroup.New, ITransaction transaction)?> ExecuteInstallersAsync(
        ILibraryItemInstaller[] installers,
        Loadout.ReadOnly loadout,
        IJobContext<InstallLoadoutItemJob> context)
    {
        foreach (var installer in installers)
        {
            var isSupported = installer.IsSupportedLibraryItem(LibraryItem);
            if (!isSupported) continue;

            var transaction = Connection.BeginTransaction();
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
            if (result.IsNotSupported)
            {
                transaction.Dispose();
                continue;
            }

            Debug.Assert(result.IsSuccess);
            return (loadoutGroup, transaction);
        }

        return null;
    }
}
