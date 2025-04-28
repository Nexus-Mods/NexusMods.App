using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.CrossPlatform.ProtocolRegistration;

namespace NexusMods.Networking.NexusWebApi;

internal class HandlerRegistration : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IProtocolRegistration _protocolRegistration;

    public HandlerRegistration(
        ILogger<HandlerRegistration> logger,
        IProtocolRegistration protocolRegistration)
    {
        _logger = logger;
        _protocolRegistration = protocolRegistration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _protocolRegistration.RegisterHandler(uriScheme: "nxm", cancellationToken: stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while registering handler for nxm links");
        }
    }
}
