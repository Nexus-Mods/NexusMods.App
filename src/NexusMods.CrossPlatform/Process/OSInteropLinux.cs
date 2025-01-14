using System.Runtime.Versioning;
using CliWrap;
using LinuxDesktopUtils.XDGDesktopPortal;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// OS interoperation for linux
/// </summary>
[SupportedOSPlatform("linux")]
internal class OSInteropLinux : AOSInterop
{
    private readonly IFileSystem _fileSystem;
    private readonly XDGOpenDependency _xdgOpenDependency;
    private readonly DesktopPortalConnectionManagerWrapper _portalWrapper;
    private readonly ILogger _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public OSInteropLinux(
        ILoggerFactory loggerFactory,
        DesktopPortalConnectionManagerWrapper portalWrapper,
        IProcessFactory processFactory,
        IFileSystem fileSystem,
        XDGOpenDependency xdgOpenDependency) : base(loggerFactory, processFactory)
    {
        _fileSystem = fileSystem;
        _xdgOpenDependency = xdgOpenDependency;
        _portalWrapper = portalWrapper;
        _logger = loggerFactory.CreateLogger<OSInteropLinux>();
    }

    /// <inheritdoc/>
    public override async Task OpenUrl(Uri url, bool logOutput = false, bool fireAndForget = false, CancellationToken cancellationToken = default)
    {
        var portal = await GetPortal();
        await portal.OpenUriAsync(url, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task OpenFile(AbsolutePath filePath, bool logOutput = false, bool fireAndForget = false, CancellationToken cancellationToken = default)
    {
        var portal = await GetPortal();
        await portal.OpenFileAsync(
            file: FilePath.From(filePath.ToNativeSeparators(_fileSystem.OS)),
            cancellationToken: cancellationToken
        );
    }

    public override async Task OpenFileInDirectory(AbsolutePath filePath, bool logOutput = false, bool fireAndForget = true, CancellationToken cancellationToken = default)
    {
        var portal = await GetPortal();
        await portal.OpenFileInDirectoryAsync(
            file: FilePath.From(filePath.ToNativeSeparators(_fileSystem.OS)),
            cancellationToken: cancellationToken
        );
    }

    private async ValueTask<OpenUriPortal> GetPortal()
    {
        var connectionManager = await _portalWrapper.GetInstance();
        var portal = await connectionManager.GetOpenUriPortalAsync();
        return portal;
    }

    /// <inheritdoc/>
    protected override Command CreateCommand(Uri uri)
    {
        // From the man page (https://man.archlinux.org/man/xdg-open.1):
        // In case of success the process launched from the .desktop file will not be forked off and therefore
        // may result in xdg-open running for a very long time.
        // This behaviour intentionally differs from most desktop specific openers to allow terminal based applications
        // to run using the same terminal xdg-open was called from.
        return _xdgOpenDependency.CreateOpenUriCommand(uri);
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
