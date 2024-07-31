using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.Jobs;
using NexusMods.Jobs.Tests.TestHelpers;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.Paths;
using Xunit;

namespace NexusMods.Jobs.Tests;

public class PersistedJobTests
{
    private readonly IConnection _connection;
    private readonly IHost _host;
    private readonly IServiceProvider _services;

    public PersistedJobTests()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(s =>
                {
                    s.AddMnemonicDB();
                    s.AddSingleton<IStoreBackend, MnemonicDB.Storage.InMemoryBackend.Backend>();
                    s.AddSingleton<DatomStoreSettings>(_ => new DatomStoreSettings() { Path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("tests_Jobs" + Guid.NewGuid()) });
                    s.AddSlowResumableJobPersistedStateModel();
                    s.AddPersistedJobStateModel();
                    s.AddWorker<SlowResumableJobWorker>();
                    s.AddSingleton<JobRestarter>();
                }
            );
        _host = host.Build();
        _services = _host.Services;
        _connection = _services.GetRequiredService<IConnection>();
    }

    [Fact]
    public async Task CanResumeJobs()
    {

        var worker = _services.GetRequiredService<SlowResumableJobWorker>();
        var job = await SlowResumableJob.Create(_connection, null!, worker, 40);

        job.Should().BeOfType<SlowResumableJob>();
        
        var castedJob = (SlowResumableJob) job;
        
        await job.StartAsync(CancellationToken.None);

        await Task.Delay(200);

        await job.PauseAsync();
        
        var allJobs = SlowResumableJobPersistedState.All(_connection.Db).ToArray();

        var jobsWithIds = allJobs.Where(j => j.AsPersistedJobState().PersistedJobStateId == castedJob.PersistedJobStateId).ToArray();
        
        jobsWithIds.Should().HaveCount(1, "because we should only have one job with the same id");
        
        var jobWithId = jobsWithIds.First();
        
        jobWithId.Current.Should().BeGreaterThan(0, "because we should have processed at least one item");
        jobWithId.Max.Should().Be(40, "because we set the max to 40");
        
        
        var restarter = _services.GetRequiredService<JobRestarter>();

        await restarter.StartAsync(CancellationToken.None);

        var st = Stopwatch.StartNew();
        while (st.Elapsed < TimeSpan.FromSeconds(10))
        {
            await Task.Delay(100);

            jobWithId = jobWithId.Rebase();
            if (jobWithId.Current == jobWithId.Max - 1)
                return;
        }
        jobWithId.Current.Should().Be(jobWithId.Max - 1, "because we should have processed all items");
    }
}
