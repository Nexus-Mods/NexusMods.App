using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Library;

internal class ExtractArchiveJobWorker : AJobWorker<ExtractArchiveJob>
{
    private readonly IFileExtractor _fileExtractor;

    public ExtractArchiveJobWorker(
        IServiceProvider serviceProvider,
        ExtractArchiveJob jobGroup) : base(jobGroup)
    {
        _fileExtractor = serviceProvider.GetRequiredService<IFileExtractor>();
    }

    protected override async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        await _fileExtractor.ExtractAllAsync(Job.FileStreamFactory, dest: Job.OutputPath, token: cancellationToken);
        return CompleteJob(Job.OutputPath);
    }
}
