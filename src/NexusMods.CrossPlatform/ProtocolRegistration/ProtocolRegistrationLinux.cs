using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging;
using NexusMods.CrossPlatform.Process;
using NexusMods.Paths;
using NexusMods.Sdk;

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

        // Note(sewer): For `processPath` we're temporarily using `_osInterop.GetOwnExe()` because
        //              paths library will replace backslashes in folder names with forward slashes.
        //              Backslashes are valid on Linux (& macOS), so we're avoiding 
        //              breaking the App here.
        // https://github.com/Nexus-Mods/NexusMods.Paths/issues/71

        // Note(sewer): xdg-utils has issues with the 'generic' fallback for `.desktop` files
        //              which will be used in non-mainstream DEs like Hyprland, Sway, i3, etc.
        //              We'll use a hack to work around this.
        //
        // See: https://gitlab.freedesktop.org/xdg/xdg-utils/-/issues/279
        //      https://github.com/Nexus-Mods/NexusMods.App/issues/3293
        //
        // So, here we're creating a wrapper script that will be used to execute the App.
        //
        // The idea here is that usernames in Linux distros are compliant with POSIX standards,
        // which allow for only alphanumeric characters, underscores, dots, and hyphens.
        // None of these characters require escaping in `.desktop` files, so quotes are not needed.
        //
        // See: https://systemd.io/USER_NAMES.
        //
        // If our username cannot contain an escape character and `XDG_DATA_HOME` is usually in
        // /home/<username>/.local/share ; then we've got a path that does not need escaping, which
        // will work around the issue with xdg-utils for the time being.
        //
        // I also added an extra 'safety' rule: 
        //  - The wrapper will only be used if the path requires escaping.
        //  - IF Path requires escaping AND `XDG_DATA_HOME` needs escaping, we log a warning.
        var processPath = Environment.ProcessPath!;
        var wrapperScriptPath = await CreateWrapperScriptIfNeeded(applicationsDirectory, processPath, cancellationToken);

        text = text.Replace(ExecuteParameterPlaceholder, wrapperScriptPath.ToString());
        text = text.Replace(TryExecuteParameterPlaceholder, EscapeDesktopFilePath(processPath));

        await filePath.WriteAllTextAsync(text, cancellationToken);
    }

    private async Task<string> CreateWrapperScriptIfNeeded(AbsolutePath applicationsDirectory, string executablePath, CancellationToken cancellationToken = default)
    {
        // If our escaped path is same as original path, we don't need the wrapper script.
        // Since that will just work.
        var escapedExecutablePath = EscapeDesktopExecFilePath(executablePath);
        if (escapedExecutablePath == executablePath)
            return escapedExecutablePath;
        
        var originalApplicationsDirectory = applicationsDirectory.ToString();
        var escapedApplicationsDirectory = EscapeDesktopExecFilePath(originalApplicationsDirectory);
        if (escapedApplicationsDirectory != originalApplicationsDirectory)
            _logger.LogWarning("XDG_DATA_HOME is in a folder that needs escaping. If login does not work, see https://gitlab.freedesktop.org/xdg/xdg-utils/-/issues/279 , https://github.com/Nexus-Mods/NexusMods.App/issues/3293 . It's out of our hands.");

        var scriptName = $"{ApplicationId}.sh";
        var scriptPath = applicationsDirectory.Combine(scriptName);
        
        _logger.LogInformation("Creating wrapper script at `{Path}`", scriptPath);

        // Create shell script content that passes all arguments to the actual executable
        var scriptContent = $"""
            #!/bin/sh
            exec "{executablePath}" "$@"
            """;
        await scriptPath.WriteAllTextAsync(scriptContent, cancellationToken);

        // Make the script executable
        var mode755 = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | // 7
           UnixFileMode.GroupRead | UnixFileMode.GroupExecute | // 5
           UnixFileMode.OtherRead | UnixFileMode.OtherExecute; // 5
        scriptPath.SetUnixFileMode(mode755);

        return scriptPath.ToString();
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
    internal static string EscapeDesktopExecFilePath(string path)
    {
        var originalPath = path;

        // Note(sewer)
        //
        // Spec says"
        //
        // > Note that the general escape rule for values of type string states that the backslash
        // > character can be escaped as ("\\") as well and that this escape rule is applied before the quoting rule
        // 
        // BUT it wasn't clear if this was for decoding or encoding.
        // Turns out it's for decoding.
        //
        // You can verify this by putting the App in a folder with a space in it.
        // The correct output is '\s', not '\\s'. Former works, latter does not.
        var escapedPath = path
            .Replace(@"\", @"\\") // and backslash character ("\")                 \ -> \\
            .Replace("\"", @"\""") // 'and escaping the double quote character'    " -> \" 
            .Replace("`", @"\`") // backtick character ("`")                       ` -> \`
            .Replace("$", @"\$"); // dollar sign ("$")                             $ -> \$

        // First apply the base rules from `string` and `localstring`.
        var escapedFinalPath = EscapeDesktopFilePath(escapedPath);

        // Note(sewer): Quoting the spec
        // 
        // > Note that the general escape rule for values of type string states that the backslash
        //   character can be escaped as ("\\") as well and that this escape rule is applied before
        //   the quoting rule. As such, to unambiguously represent a literal backslash character in
        //   a quoted argument in a desktop entry file requires the use of four successive backslash
        //   characters ("\\\\").
        // 
        // So escaping `\` twice, leading to 4 backslashes as a result of applying both functions is by design,
        // even if it may 'feel weird'.
        //
        // > Likewise, a literal dollar sign in a quoted argument in a desktop
        //   entry file is unambiguously represented with ("\\$"). 
        //
        // So we go from `$` to `\$`.
        // And then from `\$` to `\\$`.
        // 
        // As per the example in the spec, this is not a bug, this is intended behaviour, even if weird.

        // Note(sewer):
        //
        // The docs say:
        // > Arguments may be quoted in whole. If an argument contains a reserved character the argument must be quoted.
        //
        // > Reserved characters are space (" "), tab, newline, double quote, single quote ("'"), backslash character ("\"),
        // > greater-than sign (">"), less-than sign ("<"), tilde ("~"), vertical bar ("|"), ampersand ("&"), semicolon (";"),
        // > dollar sign ("$"), asterisk ("*"), question mark ("?"), hash mark ("#"), parenthesis ("(") and (")") and backtick character ("`").
        //
        // In this case, we will quote if our path has changed, else we'll leave it unquoted.
        if (escapedFinalPath == originalPath)
            return originalPath; // No need to quote if the path hasn't changed
        else
            return $"\"{escapedFinalPath}\""; // Enclose the entire path in double quotes
    }

    // Note(sewer)
    // For other fields 'string' and 'localestring':
    //
    // https://specifications.freedesktop.org/desktop-entry-spec/1.0/value-types.html
    // The escape sequences \s, \n, \t, \r, and \\ are supported for values of type string and localestring,
    // meaning ASCII space, newline, tab, carriage return, and backslash, respectively. 
    //
    // Note that 'Exec' is a string https://specifications.freedesktop.org/desktop-entry-spec/1.0/recognized-keys.html
    internal static string EscapeDesktopFilePath(string path)
    {
        return path
            .Replace(@"\", @"\\") // Escape backslashes first to avoid double escaping
            .Replace(" ", @"\s")
            .Replace("\n", @"\n")
            .Replace("\t", @"\t")
            .Replace("\r", @"\r");
    }
}
