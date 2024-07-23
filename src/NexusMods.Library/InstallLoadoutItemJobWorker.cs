using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
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
        if (!job.Installer.HasValue)
        {
            ILibraryItemInstaller? foundInstaller = null;
            var installers = job.Loadout.InstallationInstance.GetGame().LibraryItemInstallers;
            foreach (var installer in installers)
            {
                var isSupported = installer.IsSupportedLibraryItem(job.LibraryItem);
                if (!isSupported) continue;

                foundInstaller = installer;
                break;
            }

            // TODO: default to advanced installer
            if (foundInstaller is null)
            {
                return JobResult.CreateFailed($"Found no installer that supports `{job.LibraryItem.Name}` (`{job.LibraryItem.Id}`)");
            }

            job.Installer = Optional<ILibraryItemInstaller>.Create(foundInstaller);
        }

        var result = await job.Installer.Value.ExecuteAsync(job.LibraryItem, job.Transaction, job.Loadout, cancellationToken);
        var transactionResult = await job.Transaction.Commit();

        var jobResults = result.Select(x => transactionResult.Remap(x)).ToArray();
        return JobResult.CreateCompleted(jobResults);
    }
}
