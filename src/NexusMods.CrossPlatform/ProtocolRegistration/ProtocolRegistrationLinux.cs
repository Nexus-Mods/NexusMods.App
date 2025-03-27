using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform.Process;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.ProtocolRegistration;

/// <summary>
/// Protocol registration for Linux.
/// </summary>
[SupportedOSPlatform("linux")]
internal class ProtocolRegistrationLinux : IProtocolRegistration
{
    private const string ApplicationId = "com.nexusmods.app";
    private const string DesktopFile = $"{ApplicationId}.desktop";
    private const string DesktopFileResourceName = $"NexusMods.CrossPlatform.{DesktopFile}";
    private const string ExecutablePathPlaceholder = "${INSTALL_EXEC}";

    private readonly ILogger _logger;
    private readonly IProcessFactory _processFactory;
    private readonly IFileSystem _fileSystem;
    private readonly IOSInterop _osInterop;
    private readonly XDGSettingsDependency _xdgSettingsDependency;
    private readonly UpdateDesktopDatabaseDependency _updateDesktopDatabaseDependency;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ProtocolRegistrationLinux(
        ILogger<ProtocolRegistrationLinux> logger,
        IProcessFactory processFactory,
        IFileSystem fileSystem,
        IOSInterop osInterop,
        XDGSettingsDependency xdgSettingsDependency,
        UpdateDesktopDatabaseDependency updateDesktopDatabaseDependency)
    {
        _logger = logger;
        _processFactory = processFactory;
        _fileSystem = fileSystem;
        _osInterop = osInterop;
        _xdgSettingsDependency = xdgSettingsDependency;
        _updateDesktopDatabaseDependency = updateDesktopDatabaseDependency;
    }

    /// <inheritdoc/>
    public async Task RegisterHandler(string uriScheme, bool setAsDefaultHandler = true, CancellationToken cancellationToken = default)
    {
        if (ApplicationConstants.InstallationMethod != InstallationMethod.PackageManager)
        {
            var applicationsDirectory = _fileSystem.GetKnownPath(KnownPath.XDG_DATA_HOME).Combine("applications");
            _logger.LogInformation("Using applications directory `{Path}`", applicationsDirectory);

            try
            {
                await CreateDesktopFile(applicationsDirectory, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception while creating desktop file, the handler for `{Scheme}` might not work", uriScheme);
                return;
            }

            try
            {
                await UpdateMIMECacheDatabase(applicationsDirectory, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception while updating MIME cache database, see process logs for more details");
                return;
            }
        }

        if (setAsDefaultHandler)
        {
            try
            {
                await SetAsDefaultHandler(uriScheme, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception while setting the default handler for `{Scheme}`, see the process logs for more details", uriScheme);
            }
        }
    }

    private async Task CreateDesktopFile(AbsolutePath applicationsDirectory, CancellationToken cancellationToken = default)
    {
        if (!applicationsDirectory.DirectoryExists())
        {
            applicationsDirectory.CreateDirectory();
        }

        var filePath = applicationsDirectory.Combine(DesktopFile);
        var backupPath = filePath.AppendExtension(new Extension(".bak"));

        if (filePath.FileExists)
        {
            _logger.LogInformation("Moving existing desktop file from `{From}` to `{To}`", filePath, backupPath);
        }

        _logger.LogInformation("Creating desktop file at `{Path}`", filePath);

        await using var stream = typeof(ProtocolRegistrationLinux).Assembly.GetManifestResourceStream(DesktopFileResourceName);
        if (stream is null)
        {
            _logger.LogError($"Manifest resource Stream for `{DesktopFileResourceName}` is null!");
            return;
        }

        using var sr = new StreamReader(stream, encoding: Encoding.UTF8);

        var text = await sr.ReadToEndAsync(cancellationToken);
        text = text.Replace(ExecutablePathPlaceholder, EscapeWhitespaceForCli(_osInterop.GetOwnExe()));

        await filePath.WriteAllTextAsync(text, cancellationToken);
    }

    private async Task UpdateMIMECacheDatabase(AbsolutePath applicationsDirectory, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating MIME cache database");

        var command = _updateDesktopDatabaseDependency.BuildUpdateCommand(EscapeWhitespaceForCli(applicationsDirectory));
        await _processFactory.ExecuteAsync(command, cancellationToken: cancellationToken);
    }

    private async Task SetAsDefaultHandler(string uriScheme, CancellationToken cancellationToken = default)
    {
        var command = _xdgSettingsDependency.CreateSetDefaultUrlSchemeHandlerCommand(uriScheme, DesktopFile);
        await _processFactory.ExecuteAsync(command, cancellationToken: cancellationToken);
    }

    private string EscapeWhitespaceForCli(AbsolutePath path) => path.ToNativeSeparators(_fileSystem.OS).Replace(" ", @"\ ");
}
