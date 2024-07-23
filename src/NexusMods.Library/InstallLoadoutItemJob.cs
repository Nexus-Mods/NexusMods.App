using DynamicData.Kernel;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Library;

public class InstallLoadoutItemJob : AJob
{
    public InstallLoadoutItemJob(
        IJobGroup? group = default,
        IJobWorker? worker = default,
        IJobMonitor? monitor = default) : base(new MutableProgress(new IndeterminateProgress()), group, worker, monitor) { }

    public required ITransaction Transaction { get; init; }
    public required LibraryItem.ReadOnly LibraryItem { get; init; }
    public required Loadout.ReadOnly Loadout { get; init; }

    public Optional<ILibraryItemInstaller> Installer { get; set; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Transaction.Dispose();
        }

        base.Dispose(disposing);
    }
}
