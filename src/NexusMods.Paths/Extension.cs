using System.Diagnostics;
using JetBrains.Annotations;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths;

/// <summary>
/// Represents an individual file extension.
/// </summary>
[PublicAPI]
public readonly struct Extension : IEquatable<Extension>
{
    /// <summary>
    /// Use when no extension is available or specified.
    /// </summary>
    public static readonly Extension None = new("");

    /// <summary>
    /// Length of this extension.
    /// </summary>
    public int Length => _extension.Length;

    private readonly string _extension;

    /// <summary/>
    /// <param name="extension">An extension which starts with a . (dot)</param>
    public Extension(string extension)
    {
        _extension = extension;

        Debug.Assert(extension == string.Empty || extension[0] == '.', $"Extension must start with a dot: '{extension}'");
    }

    /// <summary>
    /// Creates an extension instance given a file path.
    /// </summary>
    /// <param name="path">The file path to convert to an extension.</param>
    public static Extension FromPath(string path)
    {
        var extensionSpan = PathHelpers.GetExtension(path);
        return extensionSpan.IsEmpty ? None : new Extension(extensionSpan.ToString());
    }

    /// <summary/>
    public static explicit operator string(Extension path) => path._extension;

    /// <summary/>
    public static explicit operator Extension(string path) => new(path);

    /// <summary/>
    public static bool operator ==(Extension a, Extension b)
    {
        return string.Equals(a._extension, b._extension, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary/>
    public static bool operator !=(Extension a, Extension b) => !(a == b);

    /// <inheritdoc />
    public bool Equals(Extension other)
    {
        return string.Equals(_extension, other._extension, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Extension other && Equals(other);

    /// <inheritdoc />
    public override string ToString() => _extension;

    /// <inheritdoc />
    public override int GetHashCode() => _extension.GetHashCode(StringComparison.OrdinalIgnoreCase);
}
