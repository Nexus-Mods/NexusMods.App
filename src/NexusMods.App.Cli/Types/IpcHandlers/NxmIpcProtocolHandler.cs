using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Messaging;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Networking.Downloaders;
using NexusMods.Networking.NexusWebApi.Auth;

namespace NexusMods.CLI.Types.IpcHandlers;

/// <summary>
/// a handler for nxm:// urls
/// </summary>
// ReSharper disable once InconsistentNaming
public class NxmIpcProtocolHandler : IIpcProtocolHandler
{
    /// <inheritdoc/>
    public string Protocol => "nxm";

    private readonly ILogger<NxmIpcProtocolHandler> _logger;
    private readonly DownloadService _downloadService;
    private readonly OAuth _oauth;

    /// <summary>
    /// constructor
    /// </summary>
    public NxmIpcProtocolHandler(ILogger<NxmIpcProtocolHandler> logger, DownloadService downloadService, OAuth oauth)
    {
        _logger = logger;
        _downloadService = downloadService;
        _oauth = oauth;
    }

    /// <inheritdoc/>
    public async Task Handle(string url, CancellationToken cancel)
    {
        var parsed = NXMUrl.Parse(url);
        _logger.LogDebug("Received NXM URL: {Url}", parsed);
        switch (parsed)
        {
            case NXMOAuthUrl oauthUrl:
                _oauth.AddUrl(oauthUrl);
                break;
            case NXMModUrl modUrl: 
                var task = await _downloadService.AddTask(modUrl);
                _ = task.StartAsync();
                break;
            default:
                _logger.LogWarning("Unknown NXM URL type: {Url}", parsed);
                break;
        }
    }
}

