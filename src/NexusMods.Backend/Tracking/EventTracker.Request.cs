using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Sdk;

namespace NexusMods.Backend.Tracking;

internal partial class EventTracker : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan FailurePeriod = TimeSpan.FromMinutes(1);

    private const string BaseEndpoint = "https://api-eu.mixpanel.com/track";
    private static readonly Uri Endpoint = ApplicationConstants.IsDebug ? new Uri($"{BaseEndpoint}?verbose=1") : new Uri(BaseEndpoint);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var period = Period;

            try
            {
                var success = await SendRequest(stoppingToken);
                if (!success) period = FailurePeriod;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception sending events");
                period = FailurePeriod;
            }

            await Task.Delay(delay: period, cancellationToken: stoppingToken);
        }
    }

    private async ValueTask<bool> SendRequest(CancellationToken cancellationToken)
    {
        using var bufferWriter = PrepareRequest();
        if (bufferWriter is null) return true;

        var array = bufferWriter.DangerousGetArray().Array;
        Debug.Assert(array is not null);

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Content = new ByteArrayContent(array, offset: 0, count: bufferWriter.WrittenCount)
        {
            Headers =
            {
                ContentType = new MediaTypeHeaderValue(mediaType: MediaTypeNames.Application.Json),
            },
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType: MediaTypeNames.Application.Json, quality: 1.0));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType: MediaTypeNames.Text.Plain, quality: 0.5));

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.IsSuccessStatusCode) return true;

        if (response.StatusCode is HttpStatusCode.BadRequest)
        {
            if (ApplicationConstants.IsDebug && _logger.IsEnabled(LogLevel.Debug))
            {
                var requestData = Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
                _logger.LogDebug("Failed to send events with request data `{RequestData}`", requestData);
            }
        }

        if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable)
        {
            _logger.LogWarning("Failed to send events after retrying with status code {StatusCode}", response.StatusCode);
            return false;
        }

        _logger.LogWarning("Failed to send events with status code {StatusCode}", response.StatusCode);
        return false;
    }

    private ArrayPoolBufferWriter<byte>? PrepareRequest()
    {
        _insertRingBuffer.CopyTo(_sortedReadingCopy, index: 0);
        Array.Sort(_sortedReadingCopy);

        var span = CreateSpan(_sortedReadingCopy, _highestSeenId);
        if (span.IsEmpty) return null;

        _highestSeenId = span[^1].Id;

        var minRequestSize = 2; // []
        foreach (var preparedEvent in span)
        {
            Debug.Assert(preparedEvent.IsInitialized);
            minRequestSize += preparedEvent.BufferWriter.WrittenCount;
        }

        minRequestSize += span.Length; // each comma

        var bufferWriter = new ArrayPoolBufferWriter<byte>(pool: _arrayPool, initialCapacity: minRequestSize);

        {
            var jsonWriter = GetWriter(bufferWriter);

            jsonWriter.WriteStartArray();

            foreach (var preparedEvent in span)
            {
                Debug.Assert(preparedEvent.IsInitialized);
                jsonWriter.WriteRawValue(preparedEvent.BufferWriter.WrittenSpan);
                preparedEvent.Dispose();
            }

            jsonWriter.WriteEndArray();
            ReturnWriter(jsonWriter);
        }

        return bufferWriter;
    }

    private static ReadOnlySpan<PreparedEvent> CreateSpan(PreparedEvent[] input, ulong highestSeenId)
    {
        Debug.Assert(input.Length == MaxEvents);

        // Only two possibilities:
        // 1) last highest seen ID is in the array -> start span after that element
        // 2) last highest seen ID is not in the array -> entire array has new events

        // Fast paths since IDs are monotonic increasing and we sort in ascending order
        if (input[0].Id > highestSeenId) return input;
        if (input[^1].Id <= highestSeenId) return ReadOnlySpan<PreparedEvent>.Empty;

        var index = Array.BinarySearch(input, new PreparedEvent(Id: highestSeenId + 1, null!, null!));
        if (index < 0) return ReadOnlySpan<PreparedEvent>.Empty;

        Debug.Assert(index < input.Length);
        return input.AsSpan(start: index);
    }
}
