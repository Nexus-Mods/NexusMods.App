using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Sdk;

namespace NexusMods.Networking.NexusWebApi;

internal class HandlerRegistration : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IOSInterop _osInterop;

    public HandlerRegistration(
        ILogger<HandlerRegistration> logger,
        IOSInterop osInterop)
    {
        _logger = logger;
        _osInterop = osInterop;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _osInterop.RegisterUriSchemeHandler(scheme: "nxm", cancellationToken: stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while registering handler for nxm links");
        }
    }
}
