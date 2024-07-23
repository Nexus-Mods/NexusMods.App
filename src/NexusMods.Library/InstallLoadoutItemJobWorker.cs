using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;

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

        if (result is null)
        {
            // TODO: default to advanced installer
            return JobResult.CreateFailed($"Found no installer that supports `{job.LibraryItem.Name}` (`{job.LibraryItem.Id}`)");
        }

        var transactionResult = await job.Transaction.Commit();
        var jobResults = result.Select(x => transactionResult.Remap(x)).ToArray();
        return JobResult.CreateCompleted(jobResults);
    }

    private static async ValueTask<LoadoutItem.New[]?> ExecuteInstallersAsync(
        InstallLoadoutItemJob job,
        ILibraryItemInstaller[] installers,
        CancellationToken cancellationToken)
    {
        foreach (var installer in installers)
        {
            var isSupported = installer.IsSupportedLibraryItem(job.LibraryItem);
            if (!isSupported) continue;

            // TODO: we need to retract all added entities in the transaction if the installer doesn't support the item
            var result = await installer.ExecuteAsync(job.LibraryItem, job.Transaction, job.Loadout, cancellationToken);
            if (result.Length == 0) continue;

            return result;
        }

        return null;
    }
}
