using DynamicData.Kernel;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library;

internal class AddLibraryFileJob : AJob
{
    public AddLibraryFileJob(IJobGroup? group = null, IJobWorker? worker = null)
        : base(null!, group, worker) { }

    public required ITransaction Transaction { get; init; }
    public required AbsolutePath FilePath { get; init; }
    
    public required AbsolutePath RelativeTo { get; init; }
    public required bool DoCommit { get; init; }
    public required bool DoBackup { get; init; }

    public Optional<EntityId> EntityId { get; set; }
    public Optional<bool> IsArchive { get; set; }
    public Optional<bool> HasBackup { get; set; }
    public Optional<JobResult> HashJobResult { get; set; }
    public Optional<LibraryFile.New> LibraryFile { get; set; }
    public Optional<LibraryArchive.New> LibraryArchive { get; set; }
    public Optional<TemporaryPath> ExtractionDirectory { get; set; }
    public Optional<IFileEntry[]> ExtractedFiles { get; set; }
    public Optional<ValueTuple<JobResult, IFileEntry>[]> AddExtractedFileJobResults { get; set; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Transaction.Dispose();

            if (ExtractionDirectory.HasValue)
            {
                ExtractionDirectory.Value.Dispose();
                ExtractionDirectory = Optional<TemporaryPath>.None;
            }
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (ExtractionDirectory.HasValue)
        {
            await ExtractionDirectory.Value.DisposeAsync();
            ExtractionDirectory = Optional<TemporaryPath>.None;
        }

        await base.DisposeAsyncCore();
    }
}
