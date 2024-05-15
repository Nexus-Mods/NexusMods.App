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
    public OSInteropLinux(ILoggerFactory loggerFactory, IProcessFactory processFactory, IFileSystem fileSystem)
        : base(loggerFactory, processFactory)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    protected override Command CreateCommand(Uri uri)
    {
        return Cli.Wrap("xdg-open").WithArguments(new[] { uri.ToString() }, escape: true);
    }
    
    
    /// <inheritdoc />
    public override AbsolutePath GetOwnExe()
    {
        // https://docs.appimage.org/packaging-guide/environment-variables.html#type-2-appimage-runtime
        // APPIMAGE: (Absolute) path to AppImage file (with symlinks resolved)
        var appImagePath = Environment.GetEnvironmentVariable("APPIMAGE", EnvironmentVariableTarget.Process);
        var executable = appImagePath ?? Environment.ProcessPath;
        return _fileSystem.FromUnsanitizedFullPath(FixWhitespace(executable));
    }
    
    private static string FixWhitespace(string? input)
    {
        if (input is null) return string.Empty;
        return input.Replace(" ", @"\ ");
    }
}
