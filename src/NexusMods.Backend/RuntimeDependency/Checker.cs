using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Sdk;

namespace NexusMods.Backend.RuntimeDependency;

internal class RuntimeDependencyChecker : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IRuntimeDependency[] _runtimeDependencies;

    public RuntimeDependencyChecker(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<RuntimeDependencyChecker>>();
        _runtimeDependencies = serviceProvider.GetServices<IRuntimeDependency>().ToArray();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _runtimeDependencies.ToAsyncEnumerable().ForEachAwaitWithCancellationAsync(async (dep, cancellationToken) =>
        {
            try
            {
                var information = await dep.QueryInstallationInformation(cancellationToken).ConfigureAwait(false);
                if (information.HasValue)
                {
                    _logger.LogInformation("{Name}: {Information}", dep.DisplayName, information.Value);
                } else {
                    _logger.LogWarning("{Name}: couldn't query information", dep.DisplayName);
                }
            } catch (Exception e) {
                _logger.LogError(e, "{Name}: exception while querying information", dep.DisplayName);
            }
        }, cancellationToken: stoppingToken);
    }
}
