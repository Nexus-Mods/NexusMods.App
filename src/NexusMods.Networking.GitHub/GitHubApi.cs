using System.Net.Http.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Networking.GitHub.DTOs;

namespace NexusMods.Networking.GitHub;

[PublicAPI]
internal class GitHubApi : IGitHubApi
{
    private const string BaseUrl = "https://api.github.com";

    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public GitHubApi(ILogger<GitHubApi> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async ValueTask<Release[]?> FetchReleases(string organization, string repository, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"{BaseUrl}/repos/{organization}/{repository}/releases");
        _logger.LogDebug("Fetching releases from {Uri}", uri);

        try
        {
            var releases = await _httpClient.GetFromJsonAsync<Release[]>(uri, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (releases is null)
            {
                _logger.LogWarning("Failed to deserialize releases from GitHub for `{Organization}/{Repository}`", organization, repository);
                return null;
            }

            _logger.LogDebug("Fetched `{Count}` release(s) from GitHub for `{Organization}/{Repository}`", releases.Length, organization, repository);
            return releases;
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Exception fetching releases from GitHub for `{Organization}/{Repository}`", organization, repository);
        }

        return null;
    }

    public async ValueTask<Release?> FetchLatestRelease(string organization, string repository, IComparer<Release>? comparer = null, CancellationToken cancellationToken = default)
    {
        comparer ??= ReleaseTagComparer.Instance;

        var releases = await FetchReleases(organization, repository, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (releases is null) return null;

        if (releases.Length > 1) Array.Sort(releases, comparer);
        var latest = releases[^1];

        _logger.LogDebug("Latest release from GitHub for `{Organization}/{Repository}` is `{Name}`", organization, repository, latest.Name);
        return latest;
    }
}
