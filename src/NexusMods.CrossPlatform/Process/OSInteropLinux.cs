using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// OS interoperation for linux
/// </summary>
public class OSInteropLinux : AOSInterop
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Constructor.
    /// </summary>
    public OSInteropLinux(
        ILoggerFactory loggerFactory,
        IProcessFactory processFactory,
        IFileSystem fileSystem) : base(loggerFactory, processFactory)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    protected override Command CreateCommand(Uri uri)
    {
        // From the man page (https://man.archlinux.org/man/xdg-open.1):
        // In case of success the process launched from the .desktop file will not be forked off and therefore
        // may result in xdg-open running for a very long time.
        // This behaviour intentionally differs from most desktop specific openers to allow terminal based applications
        // to run using the same terminal xdg-open was called from.

        return Cli.Wrap("xdg-open").WithArguments(new[] { uri.ToString() }, escape: true);
    }

    /// <inheritdoc />
    public override AbsolutePath GetOwnExe()
    {
        // https://docs.appimage.org/packaging-guide/environment-variables.html#type-2-appimage-runtime
        // APPIMAGE: (Absolute) path to AppImage file (with symlinks resolved)
        var appImagePath = Environment.GetEnvironmentVariable("APPIMAGE", EnvironmentVariableTarget.Process);
        if (appImagePath is null) return base.GetOwnExe();

        return _fileSystem.FromUnsanitizedFullPath(appImagePath);
    }
}
