using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Networking.GitHub;

/// <summary>
/// Service registration.
/// </summary>
[PublicAPI]
public static class Services
{
    /// <summary>
    /// Registers an implementation of <see cref="IGitHubApi"/>.
    /// </summary>
    public static IServiceCollection AddGitHubApi(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddSingleton<IGitHubApi, GitHubApi>();
    }
}
