using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader.Tests;

public class HttpDownloadJobWorkerTests
{
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly HttpDownloadJobWorker _worker;
    private readonly IConnection _connection;

    public HttpDownloadJobWorkerTests(IServiceProvider serviceProvider)
    {
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        _worker = serviceProvider.GetRequiredService<HttpDownloadJobWorker>();
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_NexusModsCDN100MFile()
    {
        const string url = "https://paris.nexus-cdn.com/100M";

        await using var outputPath = _temporaryFileManager.CreateFile();
        await using var job = await CreateJob(new Uri(url), outputPath);

        await job.StartAsync();
        var result = await job.WaitToFinishAsync();
        result.ResultType.Should().Be(JobResultType.Completed);

        outputPath.Path.FileExists.Should().BeTrue();
        outputPath.Path.FileInfo.Size.Should().Be(Size.MB * 100);

        var hash = await outputPath.Path.XxHash64Async();
        hash.Should().Be(Hash.From(0xBEEADB5B05BED390));
    }

    private async ValueTask<HttpDownloadJob> CreateJob(Uri uri, AbsolutePath destination)
    {
        using var tx = _connection.BeginTransaction();

        var state = new HttpDownloadJobPersistedState.New(tx, out var id)
        {
            Destination = destination,
            Uri = uri,
            DownloadPageUri = uri,
            PersistedJobState = new PersistedJobState.New(tx, id)
            {
                Status = JobStatus.None,
                Worker = _worker,
            },
        };

        var result = await tx.Commit();
        var job = new HttpDownloadJob(_connection, result.Remap(state), worker: _worker);
        return job;
    }
}
