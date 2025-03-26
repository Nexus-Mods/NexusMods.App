using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NexusMods.Networking.GitHub.DTOs;

namespace NexusMods.Networking.GitHub;

/// <summary>
/// Comparer for <see cref="Release"/> based on semantic versions in <see cref="Release.TagName"/>.
/// </summary>
[PublicAPI]
public class ReleaseTagComparer : IComparer<Release>
{
    /// <summary>
    /// Instance.
    /// </summary>
    public static readonly IComparer<Release> Instance = new ReleaseTagComparer();

    /// <inheritdoc/>
    public int Compare(Release? x, Release? y)
    {
        if (x is null && y is null) return 0;
        if (x is null && y is not null) return -1;
        if (x is not null && y is null) return 1;

        Debug.Assert(x is not null);
        Debug.Assert(y is not null);

        if (!x.TryGetVersion(out var a)) return -1;
        if (!y.TryGetVersion(out var b)) return -1;

        return a.CompareTo(b);
    }
}

/// <summary>
/// Extension methods.
/// </summary>
[PublicAPI]
public static class ReleaseExtensions
{
    /// <summary>
    /// Tries to parse the tag name as a semantic version.
    /// </summary>
    public static bool TryGetVersion(this Release release, [NotNullWhen(true)] out Version? version)
    {
        var tagName = release.TagName.AsSpan();
        if (tagName.StartsWith('v'))
            tagName = tagName[1..];

        return Version.TryParse(tagName, out version);
    }
}
