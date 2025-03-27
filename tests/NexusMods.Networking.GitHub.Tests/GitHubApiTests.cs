using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NexusMods.App.BuildInfo;
using Xunit;

namespace NexusMods.Networking.GitHub.Tests;

public class GitHubApiTests
{
    [Fact]
    public async Task Test_FetchReleases()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(ApplicationConstants.UserAgent);

        var api = new GitHubApi(NullLogger<GitHubApi>.Instance, client);

        if (GitHubApi.TryGetGitHubToken(out var token))
        {
            api.AuthenticationHeaderValue = new AuthenticationHeaderValue(scheme: "Bearer", token);
        }

        var releases = await api.FetchReleases("Nexus-Mods", "NexusMods.App");
        releases.Should().NotBeNullOrEmpty();
    }
}
