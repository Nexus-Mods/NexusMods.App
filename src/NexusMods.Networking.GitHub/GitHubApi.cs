using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Networking.GitHub.DTOs;

namespace NexusMods.Networking.GitHub;

[PublicAPI]
internal class GitHubApi : IGitHubApi
{
    private const string BaseUrl = "https://api.github.com";

    // https://docs.github.com/en/rest/about-the-rest-api/api-versions
    private const string HeaderApiVersion = "X-GitHub-Api-Version";
    private const string ApiVersion = "2022-11-28";

    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public AuthenticationHeaderValue? AuthenticationHeader { get; set; }

    public GitHubApi(ILogger<GitHubApi> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public static bool TryGetGitHubToken([NotNullWhen(true)] out string? token)
    {
        try
        {
            var environmentVariable = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (!string.IsNullOrWhiteSpace(environmentVariable))
            {
                token = environmentVariable;
                return true;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        token = null;
        return false;
    }

    public async ValueTask<Release[]?> FetchReleases(string organization, string repository, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"{BaseUrl}/repos/{organization}/{repository}/releases");
        _logger.LogDebug("Fetching releases from {Uri}", uri);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add(HeaderApiVersion, ApiVersion);

            if (AuthenticationHeader is not null) request.Headers.Authorization = AuthenticationHeader;

            var response = await _httpClient.SendAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub Api returned with status code `{Code}`", response.StatusCode);
                return null;
            }

            var releases = await response.Content.ReadFromJsonAsync<Release[]>(cancellationToken: cancellationToken).ConfigureAwait(false);
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
