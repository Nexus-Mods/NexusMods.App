using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Tasks;

/// <summary>
/// Represents an individual task to download and install a .nxm link.
/// </summary>
/// <remarks>
///     This task is usually created via <see cref="DownloadService.AddTask(NexusMods.Abstractions.NexusWebApi.Types.NXMUrl)"/>.
/// </remarks>
public class NxmDownloadTask : ADownloadTask
{
    private readonly INexusApiClient _nexusApiClient;

    public NxmDownloadTask(IServiceProvider provider) : base(provider)
    {
        _nexusApiClient = provider.GetRequiredService<INexusApiClient>();
    }


    protected override Task Download(AbsolutePath destination, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}
