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

        var (loadoutItems, transaction) = result.Value;
        using var tx = transaction;

        var transactionResult = await transaction.Commit();
        var jobResults = loadoutItems.Select(x => transactionResult.Remap(x)).ToArray();
        return JobResult.CreateCompleted(jobResults);
    }

    private static async ValueTask<(LoadoutItem.New[], ITransaction transaction)?> ExecuteInstallersAsync(
        InstallLoadoutItemJob job,
        ILibraryItemInstaller[] installers,
        CancellationToken cancellationToken)
    {
        foreach (var installer in installers)
        {
            var isSupported = installer.IsSupportedLibraryItem(job.LibraryItem);
            if (!isSupported) continue;

            var transaction = job.Connection.BeginTransaction();
            var result = await installer.ExecuteAsync(job.LibraryItem, transaction, job.Loadout, cancellationToken);
            if (result.Length == 0)
            {
                transaction.Dispose();
                continue;
            }

            return (result, transaction);
        }

        return null;
    }
}
