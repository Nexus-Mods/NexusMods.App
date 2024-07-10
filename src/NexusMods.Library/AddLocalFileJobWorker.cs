using System.Collections;
using System.Collections.ObjectModel;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.Jobs;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Library;

file class AddLocalFileJobWorker : AJobGroupWorker<AddLocalFileJobGroup>
{
    private readonly ILogger _logger;

    private readonly IFileExtractor _fileExtractor;

    public AddLocalFileJobWorker(
        IServiceProvider serviceProvider,
        AddLocalFileJobGroup job) : base(job)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<AddLocalFileJobGroup>>();
        _fileExtractor = serviceProvider.GetRequiredService<IFileExtractor>();
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var absolutePath = JobGroup.FilePath;
        _logger.LogInformation("Adding local file at `{Path}` to the library", absolutePath);

        if (!absolutePath.FileExists)
        {
            if (absolutePath.DirectoryExists())
            {
                FailJob($"File at `{absolutePath}` can't be added to the library because it's a directory");
            }

            FailJob($"File at `{absolutePath}` can't be added to the library because it doesn't exist");
            return;
        }

        var stream = absolutePath.Open(FileMode.Open, FileAccess.Read, FileShare.None);
        var isArchive = await _fileExtractor.CanExtract(stream);

        var hashJob = new HashJob
        {
            Stream = stream,
        };

        var hashWorker = Worker.CreateFromStaticFunction(hashJob, async static (job, cancellationToken) =>
        {
            var stream = job.Stream;
            stream.Position = 0;

            var hash = await stream.XxHash64Async(token: cancellationToken);
            stream.Position = 0;

            return hash;
        });

        var hashJobResult = await AddJobAndWaitAsync(hashJob);
    }
}
