using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;

namespace NexusMods.Networking.Downloaders;

/// <summary>
/// Services you can add to your DI container.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the networking services to your dependency injection container.
    /// </summary>
    /// <param name="services">Your DI container collection builder.</param>
    public static IServiceCollection AddDownloaders(this IServiceCollection services)
    {        
        return services.AddSingleton<DownloadService>()
            .AddSettings<DownloadSettings>()
            .AddHostedService<DownloadService>(sp=> sp.GetRequiredService<DownloadService>())
            .AddSingleton<IDownloadService>(sp=> sp.GetRequiredService<DownloadService>());
    }
}
