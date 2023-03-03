using System.Runtime.Versioning;
using System.Text;
using CliWrap;

namespace NexusMods.Common.ProtocolRegistration;

/// <summary>
/// Protocol registration for Linux.
/// </summary>
[SupportedOSPlatform("linux")]
public class ProtocolRegistrationLinux : IProtocolRegistration
{
    private const string BaseId = "nexusmods-app";

    private readonly IProcessFactory _processFactory;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="processFactory"></param>
    public ProtocolRegistrationLinux(IProcessFactory processFactory)
    {
        _processFactory = processFactory;
    }

    /// <inheritdoc/>
    public Task<string> RegisterSelf(string protocol)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<string> Register(string protocol, string friendlyName, string? commandLine = null)
    {
        throw new NotImplementedException();
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
