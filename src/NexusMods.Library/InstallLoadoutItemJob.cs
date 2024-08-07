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
        IJobMonitor? monitor = default) 
        : base(new MutableProgress(new IndeterminateProgress()), group, worker, monitor) { }

    public required IConnection Connection { get; init; }
    public required LibraryItem.ReadOnly LibraryItem { get; init; }
    public required Loadout.ReadOnly Loadout { get; init; }
    public ILibraryItemInstaller? Installer { get; init; }
}
