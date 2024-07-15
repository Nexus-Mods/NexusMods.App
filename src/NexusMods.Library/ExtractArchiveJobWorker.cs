using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Library;

[UsedImplicitly]
internal class ExtractArchiveJobWorker : AJobWorker<ExtractArchiveJob>
{
    private readonly IFileExtractor _fileExtractor;

    public ExtractArchiveJobWorker(IServiceProvider serviceProvider)
    {
        _fileExtractor = serviceProvider.GetRequiredService<IFileExtractor>();
    }

    protected override async Task<JobResult> ExecuteAsync(ExtractArchiveJob job, CancellationToken cancellationToken)
    {
        await _fileExtractor.ExtractAllAsync(job.FileStreamFactory, dest: job.OutputPath, token: cancellationToken);
        return JobResult.CreateCompleted(job.OutputPath);
    }
}
