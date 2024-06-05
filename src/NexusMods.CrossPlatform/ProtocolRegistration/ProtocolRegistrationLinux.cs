using System.Runtime.Versioning;
using System.Text;
using CliWrap;
using NexusMods.CrossPlatform.Process;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.ProtocolRegistration;

/// <summary>
/// Protocol registration for Linux.
/// </summary>
[SupportedOSPlatform("linux")]
public class ProtocolRegistrationLinux : IProtocolRegistration
{
    private const string BaseId = "nexusmods-app";

    private readonly IProcessFactory _processFactory;
    private readonly IFileSystem _fileSystem;
    private readonly IOSInterop _osInterop;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ProtocolRegistrationLinux(IProcessFactory processFactory, IFileSystem fileSystem, IOSInterop osInterop)
    {
        _processFactory = processFactory;
        _fileSystem = fileSystem;
        _osInterop = osInterop;
    }

    /// <inheritdoc/>
    public async Task<string?> RegisterSelf(string protocol)
    {
        var executable = _osInterop.GetOwnExe();

        return await Register(
            protocol,
            friendlyName: $"{BaseId}-{protocol}.desktop",
            workingDirectory: executable.Directory,
            commandLine: $"{EscapeWhitespaceForCli(executable)} protocol-invoke --url %u"
        );
    }

    private static string EscapeWhitespaceForCli(AbsolutePath path) => path.ToString().Replace(" ", @"\ ");

    /// <inheritdoc/>
    public async Task<string?> Register(string protocol, string friendlyName, string workingDirectory, string commandLine)
    {
        var applicationsFolder = _fileSystem.GetKnownPath(KnownPath.HomeDirectory)
            .Combine(".local/share/applications");

        applicationsFolder.CreateDirectory();

        var desktopEntryFile = applicationsFolder.Combine(friendlyName);

        var sb = new StringBuilder();
        sb.AppendLine("[Desktop Entry]");
        sb.AppendLine($"Name=NexusMods.App {protocol.ToUpper()} Handler");
        sb.AppendLine("Terminal=false");
        sb.AppendLine("Type=Application");
        sb.AppendLine($"Path={workingDirectory}");
        sb.AppendLine($"Exec={commandLine}");
        sb.AppendLine($"MimeType=x-scheme-handler/{protocol}");
        sb.AppendLine("NoDisplay=true");

        await desktopEntryFile.WriteAllTextAsync(sb.ToString());

        var command = Cli.Wrap("update-desktop-database")
            .WithArguments(applicationsFolder.ToString());

        await _processFactory.ExecuteAsync(command);
        return null;
    }

    /// <inheritdoc/>
    public async Task<bool> IsSelfHandler(string protocol)
    {
        var stdOutBuffer = new StringBuilder();

        var command = Cli.Wrap("xdg-settings")
            .WithArguments($"check default-url-scheme-handler {protocol} {BaseId}-{protocol}.desktop")
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        var res = await _processFactory.ExecuteAsync(command);
        if (res.ExitCode != 0) return false;

        var stdOut = stdOutBuffer.ToString();

        // might end with 0xA (LF)
        return stdOut.StartsWith("yes", StringComparison.InvariantCultureIgnoreCase);
    }
}
