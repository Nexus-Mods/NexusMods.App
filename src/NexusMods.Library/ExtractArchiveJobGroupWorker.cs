using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Library;

internal class ExtractArchiveJobGroupWorker : AJobGroupWorker<ExtractArchiveJobGroup>
{
    private readonly IFileExtractor _fileExtractor;

    public ExtractArchiveJobGroupWorker(
        IServiceProvider serviceProvider,
        ExtractArchiveJobGroup jobGroup) : base(jobGroup)
    {
        _fileExtractor = serviceProvider.GetRequiredService<IFileExtractor>();
    }

    protected override async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        await _fileExtractor.ExtractAllAsync(JobGroup.FileStreamFactory, dest: JobGroup.OutputPath, token: cancellationToken);
        return CompleteJob(JobGroup.OutputPath);
    }
}
