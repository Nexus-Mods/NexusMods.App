using NexusMods.Abstractions.Jobs;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library;

internal class AddLocalFileJobGroup : AJobGroup
{
    public AddLocalFileJobGroup(IJobGroup? group = null, IJobWorker? worker = null) : base(group, worker) { }

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
}
