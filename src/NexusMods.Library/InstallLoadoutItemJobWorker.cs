using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.AdvancedInstaller;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Library;

[UsedImplicitly]
internal class InstallLoadoutItemJobWorker : AJobWorker<InstallLoadoutItemJob>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public InstallLoadoutItemJobWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<InstallLoadoutItemJobWorker>>();
    }

    protected override async Task<JobResult> ExecuteAsync(InstallLoadoutItemJob job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var installers = job.Loadout.InstallationInstance.GetGame().LibraryItemInstallers;
        
        if (job.Installer != null)
            installers = [job.Installer];

        var result = await ExecuteInstallersAsync(job, installers, cancellationToken);

        if (!result.HasValue)
        {
            var manualInstaller = AdvancedManualInstaller.Create(_serviceProvider);
            result = await ExecuteInstallersAsync(job, [manualInstaller], cancellationToken);

            if (!result.HasValue)
            {
                return JobResult.CreateFailed($"Found no installer that supports `{job.LibraryItem.Name}` (`{job.LibraryItem.Id}`), including the advanced installer!");
            }
        }

        var (loadoutGroup, transaction) = result.Value;
        using var tx = transaction;

        // TODO(erri120): rename this entity to something unique, like "LoadoutItemInstalledFromLibrary"
        _ = new LibraryLinkedLoadoutItem.New(tx, loadoutGroup.Id)
        {
            LoadoutItemGroup = loadoutGroup,
            LibraryItemId = job.LibraryItem,
        };

        var transactionResult = await transaction.Commit();
        var jobResults = transactionResult.Remap(loadoutGroup);
        return JobResult.CreateCompleted(jobResults);
    }

    private static async ValueTask<(LoadoutItemGroup.New, ITransaction transaction)?> ExecuteInstallersAsync(
        InstallLoadoutItemJob job,
        ILibraryItemInstaller[] installers,
        CancellationToken cancellationToken)
    {
        foreach (var installer in installers)
        {
            var isSupported = installer.IsSupportedLibraryItem(job.LibraryItem);
            if (!isSupported) continue;

            var transaction = job.Connection.BeginTransaction();
            var loadoutGroup = new LoadoutItemGroup.New(transaction, out var groupId)
            {
                IsGroup = true,
                LoadoutItem = new LoadoutItem.New(transaction, groupId)
                {
                    Name = job.LibraryItem.Name,
                    LoadoutId = job.Loadout,
                },
            };

            // TODO(erri120): add safeguards to only allow groups to be added to the parent groups
            var result = await installer.ExecuteAsync(job.LibraryItem, loadoutGroup, transaction, job.Loadout, cancellationToken);
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
