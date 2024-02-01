using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Networking.Downloaders.Tasks;
using NexusMods.Networking.HttpDownloader.Tests;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Tests.Tasks;

public class HttpDownloadTaskTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LocalHttpServer _server;
    private readonly IHttpDownloader _httpDownloader;
    private readonly TemporaryFileManager _temporaryFileManager;

    public HttpDownloadTaskTests(IServiceProvider serviceProvider, LocalHttpServer server, IHttpDownloader httpDownloader, TemporaryFileManager temporaryFileManager)
    {
        _serviceProvider = serviceProvider;
        _server = server;
        _httpDownloader = httpDownloader;
        _temporaryFileManager = temporaryFileManager;
    }

    [Theory]
    [InlineData("Resources/RootedAtGameFolder/-Skyrim 202X 9.0 - Architecture-2347-9-0-1664994366.zip")]
    [InlineData("Resources/RootedAtDataFolder/-Skyrim 202X 9.0 to 9.4 - Update Ravenrock.zip")]
    [InlineData("Resources/HasEsp_InSubfolder/SkyUI_5_2_SE-12604-5-2SE_partial.zip")]
    [InlineData("Resources/HasEsp/SkyUI_5_2_SE-12604-5-2SE_partial.zip")]
    [InlineData("Resources/DataFolderWithDifferentName/-Skyrim 202X 9.0 to 9.4 - Update Ravenrock.zip")]
    public async Task DownloadModFromUrl(string url)
    {
        // This test fails if mock throws.
        // DownloadTasks report their results to IDownloadService, so we intercept them from there.
        var mock = DownloadTasksCommon.CreateMockWithConfirmFileReceive();
        var task = new HttpDownloadTask(_serviceProvider.GetRequiredService<ILogger<HttpDownloadTask>>(), _temporaryFileManager, _serviceProvider.GetRequiredService<HttpClient>(), _httpDownloader, mock);
        var makeUrl = $"{_server.Uri}{url}";
        task.Init(makeUrl);
        await task.StartAsync();
    }

    [Theory]
    [InlineData("Resources/RootedAtGameFolder/-Skyrim 202X 9.0 - Architecture-2347-9-0-1664994366.zip")]
    [InlineData("Resources/RootedAtDataFolder/-Skyrim 202X 9.0 to 9.4 - Update Ravenrock.zip")]
    [InlineData("Resources/HasEsp_InSubfolder/SkyUI_5_2_SE-12604-5-2SE_partial.zip")]
    [InlineData("Resources/HasEsp/SkyUI_5_2_SE-12604-5-2SE_partial.zip")]
    [InlineData("Resources/DataFolderWithDifferentName/-Skyrim 202X 9.0 to 9.4 - Update Ravenrock.zip")]
    public async Task ResumeDownload_AfterAppReboot(string url)
    {
        // This test fails if mock throws.
        // DownloadTasks report their results to IDownloadService, so we intercept them from there.
        var mock = DownloadTasksCommon.CreateMockWithConfirmFileReceive();
        var task = new HttpDownloadTask(_serviceProvider.GetRequiredService<ILogger<HttpDownloadTask>>(), _temporaryFileManager, _serviceProvider.GetRequiredService<HttpClient>(), _httpDownloader, mock);

        var makeUrl = $"{_server.Uri}{url}";
        task.Init(makeUrl);
        await task.StartSuspended();

        // Oops our app rebooted!
        var newTask = new HttpDownloadTask(_serviceProvider.GetRequiredService<ILogger<HttpDownloadTask>>(), _temporaryFileManager, _serviceProvider.GetRequiredService<HttpClient>(), _httpDownloader, mock);
        newTask.RestoreFromSuspend(task.ExportState());
        await newTask.Resume();
    }
}
