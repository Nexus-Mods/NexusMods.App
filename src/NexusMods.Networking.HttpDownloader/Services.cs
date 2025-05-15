using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using NexusMods.App.BuildInfo;
using Polly;

namespace NexusMods.Networking.HttpDownloader;

public static class Services
{
    /// <summary>
    /// Add the default HTTP downloader services
    /// </summary>
    public static IServiceCollection AddHttpDownloader(this IServiceCollection services)
    {
        return services.AddSingleton<HttpClient>(_ =>
        {
            var client = BuildClient();
            return client;
        });
    }

    private static HttpClient BuildClient()
    {
        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions())
            .Build();

        HttpMessageHandler handler = new ResilienceHandler(pipeline)
        {
            InnerHandler = new SocketsHttpHandler(),
        };

        var client = new HttpClient(handler)
        {
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(ApplicationConstants.UserAgent);

        return client;
    }
}
