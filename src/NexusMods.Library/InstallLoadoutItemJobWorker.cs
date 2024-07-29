using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Library;

[UsedImplicitly]
internal class InstallLoadoutItemJobWorker : AJobWorker<InstallLoadoutItemJob>
{
    private readonly ILogger _logger;

    public InstallLoadoutItemJobWorker(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<InstallLoadoutItemJobWorker>>();
    }

    protected override async Task<JobResult> ExecuteAsync(InstallLoadoutItemJob job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var installers = job.Loadout.InstallationInstance.GetGame().LibraryItemInstallers;
        var result = await ExecuteInstallersAsync(job, installers, cancellationToken);

        if (!result.HasValue)
        {
            // TODO: default to advanced installer
            return JobResult.CreateFailed($"Found no installer that supports `{job.LibraryItem.Name}` (`{job.LibraryItem.Id}`)");
        }

        var (loadoutGroup, transaction) = result.Value;
        using var tx = transaction;

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
                IsIsLoadoutItemGroupMarker = true,
                LoadoutItem = new LoadoutItem.New(transaction, groupId)
                {
                    Name = job.LibraryItem.Name,
                    LoadoutId = job.Loadout,
                },
            };

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
