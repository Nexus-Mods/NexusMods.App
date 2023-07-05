using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace NexusMods.Paths;

/// <summary>
/// Implementation of <see cref="IOSInformation"/>.
/// </summary>
[PublicAPI]
public sealed class OSInformation : IOSInformation
{
    /// <summary>
    /// Shared instance of <see cref="IOSInformation"/> using the current
    /// runtime information.
    /// </summary>
    public static readonly IOSInformation Shared = FromCurrentRuntime();

    /// <summary>
    /// Shared instance to fake a Windows installation.
    /// </summary>
    public static readonly IOSInformation FakeWindows = new OSInformation(OSPlatform.Windows);

    /// <summary>
    /// Shared instance to fake a Unix-based installation.
    /// </summary>
    public static readonly IOSInformation FakeUnix = new OSInformation(OSPlatform.Linux);

    /// <inheritdoc/>
    public OSPlatform Platform { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="platform"></param>
    public OSInformation(OSPlatform platform)
    {
        Platform = platform;
    }

    /// <summary>
    /// Creates a new <see cref="IOSInformation"/> using the current runtime
    /// information.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown when the current platform is not supported.
    /// </exception>
    public static IOSInformation FromCurrentRuntime()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new OSInformation(OSPlatform.Windows);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new OSInformation(OSPlatform.Linux);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new OSInformation(OSPlatform.OSX);
        throw new PlatformNotSupportedException($"The current platform is not supported: {RuntimeInformation.RuntimeIdentifier}");
    }

    /// <inheritdoc/>
    public override string ToString() => Platform.ToString();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is OSInformation other && Platform.Equals(other.Platform);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return Platform.GetHashCode();
    }
}
