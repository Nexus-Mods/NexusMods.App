// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
namespace NexusMods.Abstractions.NexusWebApi.DTOs;

/// <summary>
/// Metadata and rate limit information. Returned in the headers of every response.
/// </summary>
public class ResponseMetadata
{
    /// <summary>
    /// [Rate Limit] Stores the limit your <see cref="DailyRemaining"/> will be reset to once
    /// <see cref="DailyReset"/> occurs.
    /// </summary>
    public int DailyLimit { get; set; }

    /// <summary>
    /// [Rate Limit] Number of API requests available for the rest of this day period.
    /// </summary>
    public int DailyRemaining { get; set; }

    /// <summary>
    /// [Rate Limit] Stores the time when the daily limit is next reset.
    /// </summary>
    public DateTime DailyReset { get; set; }

    /// <summary>
    /// [Rate Limit] Stores the limit your <see cref="HourlyRemaining"/> will be reset to once
    /// <see cref="HourlyReset"/> occurs.
    /// </summary>
    public int HourlyLimit { get; set; }

    /// <summary>
    /// [Rate Limit] Number of API requests available for the rest of this hour period.
    /// </summary>
    public int HourlyRemaining { get; set; }

    /// <summary>
    /// [Rate Limit] Stores the time when the hourly limit is next reset.
    /// </summary>
    public DateTime HourlyReset { get; set; }

    /// <summary>
    /// Time taken to execute the request server side, in seconds.
    /// </summary>
    public double Runtime { get; set; }

    /// <summary>
    /// Extracts the response metadata from a returned HTTP header.
    /// </summary>
    public static ResponseMetadata FromHttpHeaders(HttpResponseMessage result)
    {
        var metaData = new ResponseMetadata();
        void ParseInt(string headerName, out int output)
        {
            output = default;
            if (result.Headers.TryGetValues(headerName, out var values))
                if (int.TryParse(values.First(), out var limit))
                    output = limit;
        }

        void ParseDateTime(string headerName, out DateTime output)
        {
            output = default;
            if (result.Headers.TryGetValues(headerName, out var values))
                if (DateTime.TryParse(values.First(), out var limit))
                    output = limit;
        }

        ParseInt("x-rl-daily-limit", out var dailyLimit);
        ParseInt("x-rl-daily-remaining", out var dailyRemaining);
        ParseDateTime("x-rl-daily-reset", out var dailyReset);
        ParseInt("x-rl-hourly-limit", out var hourlyLimit);
        ParseInt("x-rl-hourly-remaining", out var hourlyRemaining);
        ParseDateTime("x-rl-hourly-reset", out var hourlyReset);
        metaData.DailyLimit = dailyLimit;
        metaData.DailyRemaining = dailyRemaining;
        metaData.DailyReset = dailyReset;
        metaData.HourlyLimit = hourlyLimit;
        metaData.HourlyRemaining = hourlyRemaining;
        metaData.HourlyReset = hourlyReset;

        if (result.Headers.TryGetValues("x-runtime", out var runtimes))
            if (double.TryParse(runtimes.First(), out var reset))
                metaData.Runtime = reset;

        return metaData;
    }
}
