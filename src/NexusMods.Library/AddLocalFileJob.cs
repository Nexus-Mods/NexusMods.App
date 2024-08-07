using NexusMods.Abstractions.Jobs;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library;

internal class AddLocalFileJob : AJob
{
    public AddLocalFileJob(IJobMonitor? monitor, IJobGroup? group = null, IJobWorker? worker = null)
        : base(null!, group, worker, monitor) { }

    public required ITransaction Transaction { get; init; }
    public required AbsolutePath FilePath { get; init; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Transaction.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override ValueTask DisposeAsyncCore()
    {
        Transaction.Dispose();

        return base.DisposeAsyncCore();
    }
}
