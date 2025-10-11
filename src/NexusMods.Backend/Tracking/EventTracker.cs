using System.Buffers;
using System.Net;
using System.Text.Json;
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Paths;
using NexusMods.Sdk.Settings;
using NexusMods.Sdk.Tracking;
using Polly;
using Polly.RateLimiting;
using Polly.Retry;

namespace NexusMods.Backend.Tracking;

internal partial class EventTracker : IEventTracker
{
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IOSInformation _osInformation;
    private readonly ILoginManager _loginManager;
    private readonly HttpClient _httpClient;

    private readonly ObjectPool<Utf8JsonWriter> _jsonWriterPool;
    private readonly ArrayPool<byte> _arrayPool;

    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly JsonEncodedText _deviceId;

    public EventTracker(
        ILogger<EventTracker> logger,
        TimeProvider timeProvider,
        IOSInformation osInformation,
        ILoginManager loginManager,
        ISettingsManager settingsManager,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _deviceId = JsonEncodedText.Encode(settingsManager.Get<TrackingSettings>().DeviceId.ToString());

        _logger = logger;
        _timeProvider = timeProvider;
        _osInformation = osInformation;
        _loginManager = loginManager;
        _jsonSerializerOptions = jsonSerializerOptions;

        var jsonWriterOptions = new JsonWriterOptions
        {
            // https://developer.mixpanel.com/reference/import-events#high-level-requirements
            // "All nested object properties must have fewer than 255 keys and a max nesting depth is 3."
            // NOTE(erri120): max depth is 5 because properties already start on depth 2
            MaxDepth = 5,
            SkipValidation = false,
            Indented = false,
        };

        _jsonWriterPool = new DefaultObjectPool<Utf8JsonWriter>(new JsonWriterPoolPolicy(jsonWriterOptions));
        _arrayPool = ArrayPool<byte>.Shared;

        var resiliencePipeline = BuildPipeline(timeProvider);
        _httpClient = new HttpClient(new ResilienceHandler(resiliencePipeline)
        {
            InnerHandler = new SocketsHttpHandler(),
        });
    }

    private static ResiliencePipeline<HttpResponseMessage> BuildPipeline(TimeProvider timeProvider)
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>
        {
            TimeProvider = timeProvider,
        };

        return builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            // https://developer.mixpanel.com/reference/track-event#limits
            // "429, 502, and 503 status codes: use exponential backoff with jitter strategy (2s -> doubling until 60s with 1-5s jitter)"
            // NOTE(erri120): "doubling until 60s" is in fact linear and not exponential...
            BackoffType = DelayBackoffType.Exponential,
            MaxRetryAttempts = 3,
            UseJitter = true,
            Delay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(60),
            ShouldHandle = args => args.Outcome switch
            {
                { Exception: RateLimiterRejectedException } => PredicateResult.True(),
                { Result: { StatusCode: HttpStatusCode.TooManyRequests or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable } } => PredicateResult.True(),
                _ => PredicateResult.False(),
            },
        })
        .Build();
    }

    private Utf8JsonWriter GetWriter(ArrayPoolBufferWriter<byte> bufferWriter)
    {
        var writer = _jsonWriterPool.Get();
        writer.Reset(bufferWriter);

        return writer;
    }

    private void ReturnWriter(Utf8JsonWriter writer) => _jsonWriterPool.Return(writer);
}

internal class JsonWriterPoolPolicy : IPooledObjectPolicy<Utf8JsonWriter>
{
    private readonly JsonWriterOptions _options;

    public JsonWriterPoolPolicy(JsonWriterOptions options)
    {
        _options = options;
    }

    public Utf8JsonWriter Create() => new(FakeWriter.Instance, _options);

    public bool Return(Utf8JsonWriter writer)
    {
        writer.Flush();
        writer.Reset(FakeWriter.Instance);
        return true;
    }
}

internal class FakeWriter : IBufferWriter<byte>
{
    public static readonly IBufferWriter<byte> Instance = new FakeWriter();

    public void Advance(int count) { }
    public Memory<byte> GetMemory(int sizeHint = 0) => Memory<byte>.Empty;
    public Span<byte> GetSpan(int sizeHint = 0) => Span<byte>.Empty;
}
