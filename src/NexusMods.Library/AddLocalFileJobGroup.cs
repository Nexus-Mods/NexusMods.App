using DynamicData.Kernel;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library;

internal class AddLocalFileJobGroup : AJobGroup
{
    public required ITransaction Transaction { get; init; }
    public required AbsolutePath FilePath { get; init; }

    public Optional<EntityId> EntityId { get; set; }
    public Optional<bool> IsArchive { get; set; }
    public Optional<JobResult> HashJobResult { get; set; }
    public Optional<LibraryFile.New> LibraryFile { get; set; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Transaction.Dispose();
        }

        base.Dispose(disposing);
    }
}
