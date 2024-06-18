using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;

namespace NexusMods.SingleProcess;

/// <summary>
/// The mode to start the single process in.
/// </summary>
public enum Mode
{
    /// <summary>
    /// Main process that listens for connections and supports UI operations.
    /// </summary>
    Main,
    /// <summary>
    /// Client process that connects to the main process and supports CLI operations.
    /// </summary>
    Client,
}

/// <summary>
/// The services for the single process application.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the single process services to the service collection.
    /// </summary>
    public static IServiceCollection AddSingleProcess(this IServiceCollection services, Mode mode)
    {
        services.AddSingleton<SyncFile>();
        services.AddSettings<CliSettings>();
        switch (mode)
        {
            case Mode.Main:
                services.AddSingleton<CliServer>();
                services.AddHostedService<CliServer>();
                break;
            case Mode.Client:
                services.AddTransient<CliClient>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
        return services;
    }
}
