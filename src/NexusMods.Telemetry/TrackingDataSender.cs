using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Cysharp.Text;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Logging;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.BuildInfo;
using NexusMods.Paths;

namespace NexusMods.Telemetry;

internal sealed class TrackingDataSender : ITrackingDataSender, IDisposable
{
    // NOTE(erri120): Arbitrarily chosen limit.
    private const int Limit = 64;

    private readonly ILoginManager _loginManager;
    private readonly HttpClient _httpClient;
    private readonly ArrayBufferWriter<byte> _writer;
    private readonly ILogger<TrackingDataSender> _logger;
    private readonly IDisposable? _disposable;

    private static readonly byte[] EncodedUserAgent = CreateUserAgent();

    public TrackingDataSender(
        ILogger<TrackingDataSender> logger,
        ILoginManager loginManager,
        HttpClient httpClient,
        IObservableExceptionSource? exceptionSource = null)
    {
        _logger = logger;
        _loginManager = loginManager;
        _httpClient = httpClient;
        _writer = new ArrayBufferWriter<byte>();

        if (exceptionSource is not null && !CompileConstants.IsDebug)
        {
            _disposable = exceptionSource.Exceptions
                .Select(static msg => msg.Exception)
                .Where(static e => e is not null)
                .Subscribe(e => AddException(e!));
        }
    }

    private readonly ValueTuple<TrackingData, ulong>[] _insertRingBuffer = new ValueTuple<TrackingData, ulong>[Limit];
    private readonly ValueTuple<TrackingData, ulong>[] _sortedReadingCopy = new ValueTuple<TrackingData, ulong>[Limit];

    private ulong _lastGeneratedId;
    private ulong _highestSeenId;

    /// <summary>
    /// Inserts the event into the queue.
    /// </summary>
    public void AddEvent(EventDefinition definition, EventMetadata metadata)
    {
        Debug.Assert(metadata.IsValid());
        Insert(new TrackingData(new EventData(definition, metadata)));
    }

    /// <summary>
    /// Inserts the exception into the queue.
    /// </summary>
    public void AddException(Exception exception)
    {
        var data = ExceptionData.Create(exception);
        if (data.Count == 0) return;

        foreach (var exceptionData in data)
        {
            Insert(new TrackingData(exceptionData));
        }
    }

    private void Insert(TrackingData trackingData)
    {
        var id = Interlocked.Increment(ref _lastGeneratedId);
        var newIndex = (int)(id % Limit);
        Debug.Assert(newIndex >= 0);
        Debug.Assert(newIndex < _insertRingBuffer.Length);

        _insertRingBuffer[newIndex] = new ValueTuple<TrackingData, ulong>(trackingData, id);
    }

    public async ValueTask Run()
    {
        PrepareArrays();

        var span = PrepareSpan(_sortedReadingCopy, _highestSeenId);
        if (span.IsEmpty) return;

        _highestSeenId = span[^1].Item2;

        var userId = _loginManager.UserInfo?.UserId ?? Optional<UserId>.None;

        try
        {
            PrepareRequest(_writer, span, userId);
            await SendData(_writer.WrittenMemory, _httpClient, CancellationToken.None);
        }
        finally
        {
            _writer.ResetWrittenCount();
        }
    }

    /// <summary>
    /// Prepares the HTTP request data.
    /// </summary>
    private static void PrepareRequest(IBufferWriter<byte> writer, ReadOnlySpan<ValueTuple<TrackingData, ulong>> data, Optional<UserId> userId)
    {
        // https://developer.matomo.org/api-reference/tracking-api#bulk-tracking
        using (var sb = ZString.CreateUtf8StringBuilder(notNested: true))
        {
            sb.Append("{ \"requests\": [");
            sb.CopyTo(writer);
        }

        var isFirst = true;
        foreach (var dataIdTuple in data)
        {
            var (trackingData, _) = dataIdTuple;

            if (!isFirst)
            {
                using var sb = ZString.CreateUtf8StringBuilder(notNested: true);
                sb.Append(",");
                sb.CopyTo(writer);
            }

            isFirst = false;
            SerializeData(writer, trackingData, userId);
        }

        using (var sb = ZString.CreateUtf8StringBuilder(notNested: true))
        {
            sb.Append("] }");
            sb.CopyTo(writer);
        }
    }

    /// <summary>
    /// Serializes the data into the buffer as an UTF8 encoded string.
    /// </summary>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static void SerializeData(IBufferWriter<byte> writer, TrackingData trackingData, Optional<UserId> userId)
    {
        using var sb = ZString.CreateUtf8StringBuilder(notNested: true);
        sb.Append("\"");

        // https://developer.matomo.org/api-reference/tracking-api#supported-query-parameters
        // basics
        sb.Append("?idsite=");
        sb.Append(Constants.MatomoSiteId);
        sb.Append("&rec=1"); // Required for tracking, must be set to one
        sb.Append("&apiv=1"); // API version to use, currently always 1

        // NOTE(erri120): If you're debugging the tracking, you can uncomment these
        // lines to instantly see the request appear in matomo.
        // sb.Append("&new_visit=1"); // If set to 1, will force a new visit to be created
        // sb.Append("&queuedtracking=0"); // If set to 0, will execute the request immediately

        sb.Append("&ua="); // User agent
        AppendBytes(EncodedUserAgent);

        sb.Append("&send_image=0"); // Matomo will respond with an HTTP 204 response code instead of a GIF image

        // NOTE(erri120): The tracking API for page visits obviously expects to be used in
        // a browser context. Since the app is a desktop application we don't have URLs
        // or "web pages". It's possible to fake this with custom URLs and page titles
        // but that's something for later.

        sb.Append("&ca=1"); // Custom action for anything that isn't a page view
        // sb.Append("&url=app://loadout"); // The full URL for the current action
        // sb.Append("&action_name=Loadout"); // For page tracks: the page title
        // NOTE(erri120): maybe something for later
        // sb.Append("&res=1920x1080"); // The resolution of the device the visitor is using

        if (userId.HasValue)
        {
            sb.Append("&uid="); // User ID for logged in users
            sb.Append(userId.Value.Value);
        }

        if (trackingData.Data.IsT0) // event
        {
            // https://developer.matomo.org/api-reference/tracking-api#optional-event-tracking-info
            var (definition, metadata) = trackingData.Data.AsT0;

            sb.Append("&e_c="); // Event category
            AppendBytes(definition.SafeCategory);

            sb.Append("&e_a="); // Event action
            AppendBytes(definition.SafeAction);

            if (metadata.Name is not null)
            {
                sb.Append("&e_n="); // Event name
                AppendBytes(metadata.SafeName);
            }

            sb.Append("&h="); // The current hour (local time)
            sb.Append(metadata.CurrentTime.Hour);
            sb.Append("&m="); // The current minute (local time)
            sb.Append(metadata.CurrentTime.Minute);
            sb.Append("&s="); // The current second (local time)
            sb.Append(metadata.CurrentTime.Second);
        } else if (trackingData.Data.IsT1) // exception
        {
            // https://developer.matomo.org/api-reference/tracking-api#tracking-http-api-reference
            var (type, message, stackTrace) = trackingData.Data.AsT1;

            sb.Append("&cra="); // message
            UrlEncode(message);

            if (stackTrace is not null)
            {
                sb.Append("&cra_st="); // stack trace
                UrlEncode(stackTrace);
            }

            sb.Append("&cra_tp="); // error type
            UrlEncode(type);
        }

        sb.Append("\"");
        sb.CopyTo(writer);

        return;

        void UrlEncode(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var encoded = WebUtility.UrlEncodeToBytes(bytes, offset: 0, count: bytes.Length);
            AppendBytes(encoded);
        }

        void AppendBytes(byte[] data)
        {
            var input = data.AsSpan();
            var destination = sb.GetSpan(sizeHint: input.Length);
            input.CopyTo(destination);
            sb.Advance(input.Length);
        }
    }

    private async ValueTask SendData(ReadOnlyMemory<byte> data, HttpClient httpClient, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Constants.MatomoTrackingEndpoint);
        request.Content = new ReadOnlyMemoryContent(data);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);

        try
        {
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Exception sending tracking data");
        }
    }

    /// <summary>
    /// Copies the queued events into a secondary array and sorts them by id.
    /// </summary>
    private void PrepareArrays()
    {
        _insertRingBuffer.CopyTo(_sortedReadingCopy, index: 0);
        Array.Sort(_sortedReadingCopy, static (a, b) => a.Item2.CompareTo(b.Item2));
    }

    /// <summary>
    /// Creates a slice of the array to exclude already seen data.
    /// </summary>
    private static ReadOnlySpan<ValueTuple<TrackingData, ulong>> PrepareSpan(ValueTuple<TrackingData, ulong>[] input, ulong highestSeenId)
    {
        Debug.Assert(input.Length == Limit);

        var sliceStartIndex = 0;
        for (var i = 0; i < Limit; i++)
        {
            var id = input[i].Item2;
            if (id > highestSeenId) break;
            sliceStartIndex += 1;
        }

        if (sliceStartIndex >= Limit) return ReadOnlySpan<ValueTuple<TrackingData, ulong>>.Empty;
        return input.AsSpan(start: sliceStartIndex);
    }

    internal static byte[] CreateUserAgent()
    {
        var raw = CreateUserAgent(OSInformation.Shared.Platform, Environment.OSVersion, RuntimeInformation.OSArchitecture);
        var bytes = Encoding.UTF8.GetBytes(raw);
        return WebUtility.UrlEncodeToBytes(bytes, offset: 0, count: bytes.Length);
    }

    /// <summary>
    /// Creates a user-agent than Matomo understands.
    /// </summary>
    private static string CreateUserAgent(OSPlatform platform, OperatingSystem osVersion, Architecture architecture)
    {
        // NOTE(erri120): Matomo user-agent parsing is done via
        // https://github.com/matomo-org/device-detector
        // Welcome to the shitty world of user-agents, where due to legacy concerns,
        // everyone just puts everything into it and nothing makes sense.
        // As such, the following strings might not make a lot of sense at first,
        // so check the links to see the parsed results.
        // The only thing we really care about is the correct OS name (Linux, Windows, Mac)
        // and the device type (desktop). Everything else is basically irrelevant.

        // https://devicedetector.lw1.at/Mozilla%2F5.0%20(X11;%20Linux%20x86_64)
        if (platform == OSPlatform.Linux)
        {
            return "Mozilla/5.0 (X11; Linux x86_64)";
        }

        // https://devicedetector.lw1.at/Mozilla%2F5.0%20(Windows%20NT%2010.0;%20Win64;%20x64)
        if (platform == OSPlatform.Windows)
        {
            return "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        }

        // https://devicedetector.lw1.at/Mozilla%2F5.0%20(Macintosh;%20Intel%20Mac%20OS%20X%2014.7)
        if (platform == OSPlatform.OSX)
        {
            return $"Mozilla/5.0 (Macintosh; Intel Mac OS X {osVersion.Version.ToString(fieldCount: 2)})";
        }

        throw new NotSupportedException();
    }

    public void Dispose()
    {
        _disposable?.Dispose();
    }
}
