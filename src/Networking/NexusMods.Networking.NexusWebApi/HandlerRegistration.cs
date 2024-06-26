using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.CrossPlatform.ProtocolRegistration;

namespace NexusMods.Networking.NexusWebApi;

internal class HandlerRegistration : IHostedService
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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _protocolRegistration.RegisterHandler(uriScheme: "nxm", cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception while registering handler for nxm links");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
