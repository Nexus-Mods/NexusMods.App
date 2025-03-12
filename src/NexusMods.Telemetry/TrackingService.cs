using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexusMods.Telemetry;

[PublicAPI]
public class TrackingService : BackgroundService
{
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(seconds: 5);

    private readonly ILogger _logger;
    private readonly ITrackingDataSender _trackingDataSender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public TrackingService(ILogger<TrackingService> logger, ITrackingDataSender trackingDataSender)
    {
        _logger = logger;
        _trackingDataSender = trackingDataSender;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Tracking.EventSender = _trackingDataSender;
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _trackingDataSender.Run().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception sending events");
            }

            try
            {
                await Task.Delay(delay: Delay, cancellationToken: stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }
}
