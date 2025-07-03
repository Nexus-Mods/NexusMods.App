using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// Defines a parser for a specific option type.
/// </summary>
[PublicAPI]
public interface IOptionParser<T> where T : notnull
{
    /// <summary>
    /// Parses the value into the option type. If the value is invalid, the error message should be set
    /// and <c>false</c> should be returned. Otherwise, <c>true</c> should be returned.
    /// </summary>
    bool TryParse(string toParse, [NotNullWhen(true)] out T? value, [NotNullWhen(false)] out string? error);
}
