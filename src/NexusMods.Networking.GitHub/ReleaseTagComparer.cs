using System.Diagnostics;
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
        if (!Version.TryParse(x!.TagName, out var a)) return -1;
        if (!Version.TryParse(y!.TagName, out var b)) return 1;

        return a.CompareTo(b);
    }
}
