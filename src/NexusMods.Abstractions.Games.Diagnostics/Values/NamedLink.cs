using JetBrains.Annotations;

namespace NexusMods.Abstractions.Diagnostics.Values;

/// <summary>
/// Represents a named link.
/// </summary>
[PublicAPI]
public readonly record struct NamedLink(string Name, Uri Uri);

/// <summary>
/// Extension methods for <see cref="Uri"/>.
/// </summary>
[PublicAPI]
public static class UriExtensions
{
    /// <summary>
    /// Converts a <see cref="Uri"/> into a <see cref="NamedLink"/>.
    /// </summary>
    public static NamedLink WithName(this Uri uri, string name) => new(name, uri);
}
