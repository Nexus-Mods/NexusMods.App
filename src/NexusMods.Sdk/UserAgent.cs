using System.Net.Http.Headers;
using JetBrains.Annotations;

namespace NexusMods.Sdk;

/// <summary>
/// Represents a user agent.
/// </summary>
/// <param name="ApplicationName">The application name part of the user agent</param>
/// <param name="ApplicationVersion">The application version part of the user agent</param>
[PublicAPI]
public record UserAgent(string ApplicationName, string ApplicationVersion)
{
    private readonly string _userAgentString = $"{ApplicationName}/{ApplicationVersion}";

    /// <inheritdoc/>
    public override string ToString() => _userAgentString;

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    public static implicit operator ProductInfoHeaderValue(UserAgent userAgent) => new(userAgent.ApplicationName, userAgent.ApplicationVersion);
}
