using JetBrains.Annotations;
using NexusMods.Networking.GitHub.DTOs;

namespace NexusMods.Networking.GitHub;

/// <summary>
/// Wrapper for the GitHub API.
/// </summary>
[PublicAPI]
public interface IGitHubApi
{
    /// <summary>
    /// Fetches the latest releases.
    /// </summary>
    ValueTask<Release[]?> FetchReleases(string organization, string repository, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the latest release.
    /// </summary>
    ValueTask<Release?> FetchLatestRelease(string organization, string repository, IComparer<Release>? comparer = null, CancellationToken cancellationToken = default);
}
