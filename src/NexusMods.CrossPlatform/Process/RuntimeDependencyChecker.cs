using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexusMods.CrossPlatform.Process;

[UsedImplicitly]
internal class RuntimeDependencyChecker : BackgroundService
{
    private readonly IRuntimeDependency[] _runtimeDependencies;
    private readonly ILogger _logger;

    public RuntimeDependencyChecker(IServiceProvider serviceProvider)
    {
        _runtimeDependencies = serviceProvider.GetServices<IRuntimeDependency>().ToArray();
        _logger = serviceProvider.GetRequiredService<ILogger<RuntimeDependencyChecker>>();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            foreach (var dep in _runtimeDependencies)
            {
                if (stoppingToken.IsCancellationRequested) return;

                try
                {
                    var information = await dep.QueryInstallationInformation(stoppingToken).ConfigureAwait(false);
                    if (information.HasValue)
                    {
                        _logger.LogInformation("{Name}: {Information}", dep.DisplayName, information.Value);
                    } else {
                        _logger.LogWarning("{Name}: couldn't query information", dep.DisplayName);
                    }
                } catch (Exception e) {
                    _logger.LogError(e, "{Name}: exception while querying information", dep.DisplayName);
                }
            }
        }, stoppingToken);
    }
}
