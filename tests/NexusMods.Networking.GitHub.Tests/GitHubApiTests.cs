using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.App.BuildInfo;
using Xunit;
using Xunit.Abstractions;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Networking.GitHub.Tests;

file class TestOutputHelperAccessor : ITestOutputHelperAccessor
{
    public ITestOutputHelper? Output { get; set; }
}

[Trait("RequiresNetworking", "True")]
public class GitHubApiTests
{
    private readonly IServiceProvider _serviceProvider;

    public GitHubApiTests(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new HostBuilder().ConfigureServices(serviceCollection => serviceCollection
            .AddSingleton<ITestOutputHelperAccessor>(_ => new TestOutputHelperAccessor
            {
                Output = testOutputHelper,
            })
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug)))
            .Build()
            .Services;
    }

    [Fact]
    public async Task Test_FetchReleases()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(ApplicationConstants.UserAgent);

        var logger = _serviceProvider.GetRequiredService<ILogger<GitHubApi>>();
        var api = new GitHubApi(logger, client);

        // if (GitHubApi.TryGetGitHubToken(out var token))
        // {
        //     api.AuthenticationHeaderValue = new AuthenticationHeaderValue(scheme: "token", token);
        // }

        var releases = await api.FetchReleases("Nexus-Mods", "NexusMods.App");
        releases.Should().NotBeNullOrEmpty();
    }
}
