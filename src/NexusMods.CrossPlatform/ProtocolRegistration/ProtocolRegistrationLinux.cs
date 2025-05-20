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
    private const string ExecuteParameterPlaceholder = "${INSTALL_EXEC}";
    private const string TryExecuteParameterPlaceholder = "${INSTALL_TRYEXEC}";

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
        var canWriteDesktopFile = ApplicationConstants.InstallationMethod is InstallationMethod.AppImage or InstallationMethod.Manually;
        var canRegisterAsDefault = ApplicationConstants.InstallationMethod is not InstallationMethod.Flatpak and not InstallationMethod.PackageManager;

        if (canWriteDesktopFile)
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

        if (setAsDefaultHandler && canRegisterAsDefault)
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
        // Note(sewer): Temporarily using `_osInterop.GetOwnExe()` because paths library will replace backslashes
        //              in path with forward slashes. And backslash is a valid character in file/folder names.
        // <INSERT ISSUE LINK HERE>
        var processPath = Environment.ProcessPath!;
        text = text.Replace(ExecuteParameterPlaceholder, EscapeDesktopExecFilePath(processPath));
        text = text.Replace(TryExecuteParameterPlaceholder, EscapeDesktopFilePath(processPath));

        await filePath.WriteAllTextAsync(text, cancellationToken);
    }

    private async Task UpdateMIMECacheDatabase(AbsolutePath applicationsDirectory, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating MIME cache database");

        var command = _updateDesktopDatabaseDependency.BuildUpdateCommand($"\"{applicationsDirectory}\"");
        await _processFactory.ExecuteAsync(command, cancellationToken: cancellationToken);
    }

    private async Task SetAsDefaultHandler(string uriScheme, CancellationToken cancellationToken = default)
    {
        // Note(sewer): This adds `MimeType=x-scheme-handler/nxm;` to the protocol handler entry
        //              so if we supply this in the existing `.desktop` file running this would make
        //              a duplicate.
        var command = _xdgSettingsDependency.CreateSetDefaultUrlSchemeHandlerCommand(uriScheme, DesktopFile);
        await _processFactory.ExecuteAsync(command, cancellationToken: cancellationToken);
    }

    // Note(sewer)
    // For the exec field. Quoted relevant parts below.
    // https://specifications.freedesktop.org/desktop-entry-spec/latest/exec-variables.html
    //
    // Arguments may be quoted in whole. If an argument contains a reserved character the argument must be quoted.
    // The rules for quoting of arguments is also applicable to the executable name or path of the executable
    // program as provided.
    //
    // Quoting must be done by enclosing the argument between double quotes and escaping the double quote
    // character, backtick character ("`"), dollar sign ("$") and backslash character ("\") by preceding
    // it with an additional backslash character.
    private string EscapeDesktopExecFilePath(string path)
    {
        // Note(sewer): Both the base rules for `EscapeDesktopFilePath` and
        // those in `EscapeDesktopExecFilePath` (as documented above both functions) 
        // apply here. At first I was unsure about this, so I've done rigorous testing
        // with real DEs.

        // First apply the base rules from `string` and `localstring`.
        var escapedBasePath = EscapeDesktopFilePath(path);

        // Note(sewer): 
        //
        // The docs say:
        // > Arguments may be quoted in whole. If an argument contains a reserved character the argument must be quoted.
        //
        // > Reserved characters are space (" "), tab, newline, double quote, single quote ("'"), backslash character ("\"),
        // > greater-than sign (">"), less-than sign ("<"), tilde ("~"), vertical bar ("|"), ampersand ("&"), semicolon (";"),
        // > dollar sign ("$"), asterisk ("*"), question mark ("?"), hash mark ("#"), parenthesis ("(") and (")") and backtick character ("`"). 
        
        // 'by preceding it with an additional backslash character.'
        var escapedPath = escapedBasePath
            .Replace("\"", @"\""") // 'and escaping the double quote character'
            .Replace("`", @"\`") // backtick character ("`")
            .Replace("$", @"\$") // dollar sign ("$")
            .Replace(@"\", @"\\"); // and backslash character ("\")
   
        // Note(sewer): Quoting the spec
        // 
        // > Note that the general escape rule for values of type string states that the backslash
        //   character can be escaped as ("\\") as well and that this escape rule is applied before
        //   the quoting rule. As such, to unambiguously represent a literal backslash character in
        //   a quoted argument in a desktop entry file requires the use of four successive backslash
        //   characters ("\\\\").
        // 
        // So escaping `\` twice, leading to 4 backslashes as a result of applying both functions is by design.
        
        // Note(sewer): The spec says
        //
        // > Likewise, a literal dollar sign in a quoted argument in a desktop
        //   entry file is unambiguously represented with ("\\$"). 
        //
        // So we go from `$` to `\$`.
        // And then from `\$` to `\\$`.
        // 
        // As per the example in the spec, this is not a bug, this is intended behaviour.
        
        // Enclose the entire path in double quotes
        return $"\"{escapedPath}\"";
    }

    // Note(sewer)
    // For other fields 'string' and 'localestring':
    //
    // https://specifications.freedesktop.org/desktop-entry-spec/1.0/value-types.html
    // The escape sequences \s, \n, \t, \r, and \\ are supported for values of type string and localestring,
    // meaning ASCII space, newline, tab, carriage return, and backslash, respectively. 
    //
    // Note that 'Exec' is a string https://specifications.freedesktop.org/desktop-entry-spec/1.0/recognized-keys.html
    private string EscapeDesktopFilePath(string path)
    {
        return path
            .Replace(@"\", @"\\") // Escape backslashes first to avoid double escaping
            .Replace(" ", @"\s")
            .Replace("\n", @"\n")
            .Replace("\t", @"\t")
            .Replace("\r", @"\r");
    }
}
