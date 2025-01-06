using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Runtime.InteropServices;
using Cysharp.Text;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;

namespace NexusMods.Telemetry;

[PublicAPI]
public interface IEventSender
{
    void AddEvent(EventDefinition definition, EventMetadata metadata);

    ValueTask Run();
}

internal class EventSender : IEventSender
{
    // NOTE(erri120): Arbitrarily chosen limit.
    private const int Limit = 64;

    private readonly ILoginManager _loginManager;
    private readonly HttpClient _httpClient;
    private readonly ArrayBufferWriter<byte> _writer;

    public EventSender(ILoginManager loginManager, HttpClient httpClient)
    {
        _loginManager = loginManager;
        _httpClient = httpClient;
        _writer = new ArrayBufferWriter<byte>();
    }

    private readonly ValueTuple<EventDefinition, EventMetadata, ulong>[] _insertRingBuffer = new ValueTuple<EventDefinition, EventMetadata, ulong>[Limit];
    private readonly ValueTuple<EventDefinition, EventMetadata, ulong>[] _sortedReadingCopy = new ValueTuple<EventDefinition, EventMetadata, ulong>[Limit];

    private ulong _lastGeneratedId;
    private ulong _highestSeenId;

    /// <summary>
    /// Inserts the event into the queue.
    /// </summary>
    public void AddEvent(EventDefinition definition, EventMetadata metadata)
    {
        // NOTE(erri120): Since these are just tracking events, it's okay if we
        // loose some of them. As such, a simple "ring buffer" represented by an
        // array is completely fine and this shouldn't impact performance for
        // any consumer inserting events.
        Debug.Assert(metadata.IsValid());

        var id = Interlocked.Increment(ref _lastGeneratedId);
        var newIndex = (int)(id % Limit);
        Debug.Assert(newIndex >= 0);
        Debug.Assert(newIndex < _insertRingBuffer.Length);

        var tuple = new ValueTuple<EventDefinition, EventMetadata, ulong>(definition, metadata, id);
        _insertRingBuffer[newIndex] = tuple;
    }

    public async ValueTask Run()
    {
        PrepareArrays();

        var span = PrepareSpan(_sortedReadingCopy, _highestSeenId);
        if (span.IsEmpty) return;

        _highestSeenId = span[^1].Item3;

        var userId = _loginManager.UserInfo?.UserId ?? Optional<UserId>.None;

        try
        {
            PrepareRequest(_writer, span, userId);
            await SendEvents(_writer.WrittenMemory, _httpClient, CancellationToken.None);
        }
        finally
        {
            _writer.ResetWrittenCount();
        }
    }

    /// <summary>
    /// Prepares the HTTP request data.
    /// </summary>
    private static void PrepareRequest(IBufferWriter<byte> writer, ReadOnlySpan<ValueTuple<EventDefinition, EventMetadata, ulong>> events, Optional<UserId> userId)
    {
        // https://developer.matomo.org/api-reference/tracking-api#bulk-tracking
        using (var sb = ZString.CreateUtf8StringBuilder(notNested: true))
        {
            sb.Append("{ \"requests\": [");
            sb.CopyTo(writer);
        }

        var isFirst = true;
        foreach (var eventTuple in events)
        {
            if (!isFirst)
            {
                using var sb = ZString.CreateUtf8StringBuilder(notNested: true);
                sb.Append(",");
                sb.CopyTo(writer);
            }

            isFirst = false;
            SerializeEvent(writer, eventTuple, userId);
        }

        using (var sb = ZString.CreateUtf8StringBuilder(notNested: true))
        {
            sb.Append("] }");
            sb.CopyTo(writer);
        }
    }

    /// <summary>
    /// Serializes the event into the buffer as an UTF8 encoded string.
    /// </summary>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static void SerializeEvent(IBufferWriter<byte> writer, ValueTuple<EventDefinition, EventMetadata, ulong> tuple, Optional<UserId> userId)
    {
        var (definition, metadata, _) = tuple;
        using var sb = ZString.CreateUtf8StringBuilder(notNested: true);
        sb.Append("\"");

        // https://developer.matomo.org/api-reference/tracking-api#supported-query-parameters
        // basics
        sb.Append("?idsite=");
        sb.Append(Constants.MatomoSiteId);
        sb.Append("&rec=1"); // Required for tracking, must be set to one
        sb.Append("&apiv=1"); // API version to use, currently always 1

        // NOTE(erri120): The device detector they have really doesn't like us
        // https://github.com/matomo-org/device-detector
        // test: https://lx3rzx16x9.csb.app
        // sb.Append("&ua="); // User agent
        // sb.Append(ApplicationConstants.UserAgent);

        sb.Append("&send_image=0"); // Matomo will respond with an HTTP 204 response code instead of a GIF image

        // NOTE(erri120): The tracking API for page visits obviously expects to be used in
        // a browser context. Since the app is a desktop application we don't have URLs
        // or "web pages". It's possible to fake this with custom URLs and page titles
        // but that's something for later.

        sb.Append("&ca=1"); // Custom action for anything that isn't a page view
        // sb.Append("&url=app://loadout"); // The full URL for the current action
        // sb.Append("&action_name=Loadout"); // For page tracks: the page title

        // NOTE(erri120): If you're debugging the tracking, you can uncomment these
        // lines to instantly see the request appear in matomo.
        // sb.Append("&new_visit=1"); // If set to 1, will force a new visit to be created
        // sb.Append("&queuedtracking=0"); // If set to 0, will execute the request immediately

        if (userId.HasValue)
        {
            sb.Append("&uid="); // User ID for logged in users
            sb.Append(userId.Value.Value);
        }

        sb.Append("&e_c="); // Event category
        {
            var input = definition.SafeCategory.AsSpan();
            var destination = sb.GetSpan(sizeHint: input.Length);
            input.CopyTo(destination);
            sb.Advance(input.Length);
        }

        sb.Append("&e_a="); // Event action
        {
            var input = definition.SafeAction.AsSpan();
            var destination = sb.GetSpan(sizeHint: input.Length);
            input.CopyTo(destination);
            sb.Advance(input.Length);

        }

        if (metadata.Name is not null)
        {
            sb.Append("&e_n="); // Event name
            var input = metadata.SafeName.AsSpan();
            var destination = sb.GetSpan(sizeHint: input.Length);
            input.CopyTo(destination);
            sb.Advance(input.Length);
        }

        sb.Append("&h="); // The current hour (local time)
        sb.Append(metadata.CurrentTime.Hour);
        sb.Append("&m="); // The current minute (local time)
        sb.Append(metadata.CurrentTime.Minute);
        sb.Append("&s="); // The current second (local time)
        sb.Append(metadata.CurrentTime.Second);

        // NOTE(erri120): maybe something for later
        // sb.Append("&res=1920x1080"); // The resolution of the device the visitor is using

        sb.Append("\"");
        sb.CopyTo(writer);
    }

    private static async ValueTask SendEvents(ReadOnlyMemory<byte> data, HttpClient httpClient, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Constants.MatomoTrackingEndpoint);
        request.Content = new ReadOnlyMemoryContent(data);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Copies the queued events into a secondary array and sorts them by id.
    /// </summary>
    private void PrepareArrays()
    {
        _insertRingBuffer.CopyTo(_sortedReadingCopy, index: 0);
        Array.Sort(_sortedReadingCopy, static (a, b) => a.Item3.CompareTo(b.Item3));
    }

    /// <summary>
    /// Creates a slice of the array to exclude already seen events.
    /// </summary>
    private static ReadOnlySpan<ValueTuple<EventDefinition, EventMetadata, ulong>> PrepareSpan(ValueTuple<EventDefinition, EventMetadata, ulong>[] input, ulong highestSeenId)
    {
        Debug.Assert(input.Length == Limit);

        var sliceStartIndex = 0;
        for (var i = 0; i < Limit; i++)
        {
            var id = input[i].Item3;
            if (id > highestSeenId) break;
            sliceStartIndex += 1;
        }

        if (sliceStartIndex >= Limit) return ReadOnlySpan<ValueTuple<EventDefinition, EventMetadata, ulong>>.Empty;
        return input.AsSpan(start: sliceStartIndex);
    }

    /// <summary>
    /// Creates a user-agent than Matomo understands.
    /// </summary>
    internal static string CreateUserAgent(OSPlatform platform, OperatingSystem osVersion)
    {
        // https://lx3rzx16x9.csb.app/#Arch%20Linux%206.12
        if (platform == OSPlatform.Linux)
        {
            return $"Arch Linux {osVersion.Version.ToString(fieldCount: 2)}";
        }

        // https://lx3rzx16x9.csb.app/#Windows%20NT%2010.0
        if (platform == OSPlatform.Windows)
        {
            return $"Windows NT {osVersion.Version.ToString(fieldCount: 2)}";
        }

        // https://lx3rzx16x9.csb.app/#Macintosh;%20Intel%20Mac%20OS%20X%2010_1
        if (platform == OSPlatform.OSX)
        {
            return $"Macintosh; Intel Mac OS X {osVersion.Version.Major}_{osVersion.Version.Minor}";
        }

        throw new NotSupportedException();
    }
}
