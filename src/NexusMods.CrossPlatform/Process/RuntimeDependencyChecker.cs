using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexusMods.CrossPlatform.Process;

[UsedImplicitly]
internal class RuntimeDependencyChecker : IHostedService
{
    private readonly IRuntimeDependency[] _runtimeDependencies;
    private readonly ILogger _logger;

    public RuntimeDependencyChecker(IServiceProvider serviceProvider)
    {
        _runtimeDependencies = serviceProvider.GetServices<IRuntimeDependency>().ToArray();
        _logger = serviceProvider.GetRequiredService<ILogger<RuntimeDependencyChecker>>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var dep in _runtimeDependencies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var information = await dep.QueryInstallationInformation(cancellationToken);
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
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
