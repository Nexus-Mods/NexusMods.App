using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Networking.Downloaders.Tasks;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Tests.Tasks;

[Trait("RequiresNetworking", "True")]
public class NxmDownloadTaskTests
{
    private readonly IHttpDownloader _httpDownloader;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly Client _nexusClient;

    public NxmDownloadTaskTests(IServiceProvider serviceProvider, IHttpDownloader httpDownloader, TemporaryFileManager temporaryFileManager, Client nexusClient)
    {
        _httpDownloader = httpDownloader;
        _temporaryFileManager = temporaryFileManager;
        _nexusClient = nexusClient;
    }

    [Theory]
    [InlineData("cyberpunk2077", 107, 33156)]
    public async Task DownloadModFromNxm(string gameDomain, ulong modId, ulong fileId)
    {
        // This test requires Premium. If it fails w/o Premium, ignore that.

        // This test fails if mock throws.
        // DownloadTasks report their results to IDownloadService, so we intercept them from there.
        var downloadService = DownloadTasksCommon.CreateMockWithConfirmFileReceive();
        var task = new NxmDownloadTask(_temporaryFileManager, _nexusClient, _httpDownloader, downloadService);

        var uri = $"nxm://{gameDomain}/mods/{modId}/files/{fileId}";
        task.Init(NXMUrl.Parse(uri));
        await task.StartAsync();
    }

    [Theory]
    [InlineData("cyberpunk2077", 107, 33156)]
    public async Task ResumeDownload(string gameDomain, ulong modId, ulong fileId)
    {
        // This test requires Premium. If it fails w/o Premium, ignore that.

        // This test fails if mock throws.
        // DownloadTasks report their results to IDownloadService, so we intercept them from there.
        var downloadService = DownloadTasksCommon.CreateMockWithConfirmFileReceive();
        var task = new NxmDownloadTask(_temporaryFileManager, _nexusClient, _httpDownloader, downloadService);

        var uri = $"nxm://{gameDomain}/mods/{modId}/files/{fileId}";
        task.Init(NXMUrl.Parse(uri));
        await task.StartSuspended();
        await task.Resume();
    }

    private async Task DelayedSuspend(NxmDownloadTask task)
    {
        await Task.Delay(500);
        task.Suspend();
    }

    [Theory]
    [InlineData("cyberpunk2077", 107, 33156)]
    public async Task SuspendAndResumeDownload(string gameDomain, ulong modId, ulong fileId)
    {
        // This test requires Premium. If it fails w/o Premium, ignore that.

        // This test fails if mock throws.
        // DownloadTasks report their results to IDownloadService, so we intercept them from there.
        var downloadService = DownloadTasksCommon.CreateMockWithConfirmFileReceive();
        var task = new NxmDownloadTask(_temporaryFileManager, _nexusClient, _httpDownloader, downloadService);

        var uri = $"nxm://{gameDomain}/mods/{modId}/files/{fileId}";
        task.Init(NXMUrl.Parse(uri));
        try
        {
            await Task.WhenAll(task.StartAsync(), DelayedSuspend(task));
        }
        catch(TaskCanceledException)
        {
            // Ignore
        }

        await task.Resume();
    }

    [Theory]
    [InlineData("cyberpunk2077", 107, 33156)]
    public async Task ResumeDownload_AfterAppReboot(string gameDomain, ulong modId, ulong fileId)
    {
        // This test requires Premium. If it fails w/o Premium, ignore that.

        // This test fails if mock throws.
        // DownloadTasks report their results to IDownloadService, so we intercept them from there.
        var downloadService = DownloadTasksCommon.CreateMockWithConfirmFileReceive();
        var task = new NxmDownloadTask(_temporaryFileManager, _nexusClient, _httpDownloader, downloadService);

        var uri = $"nxm://{gameDomain}/mods/{modId}/files/{fileId}";
        task.Init(NXMUrl.Parse(uri));
        await task.StartSuspended();

        // Oops our app rebooted!
        var newTask = new NxmDownloadTask(_temporaryFileManager, _nexusClient, _httpDownloader, downloadService);
        newTask.RestoreFromSuspend(task.ExportState());
        await newTask.Resume();
    }
}
