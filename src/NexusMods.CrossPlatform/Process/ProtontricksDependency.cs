using System.Runtime.InteropServices;
using CliWrap;
namespace NexusMods.CrossPlatform.Process;

/// <inheritdoc />
/// <remarks>
/// There is an implicit assumption made here that if `protontricks` is installed,
/// then `protontricks-launch` is also available. Should it not be available, then
/// it means that `protontricks` was incorrectly packaged by the package maintainer
/// for a specific repo.
/// </remarks>
public class ProtontricksDependency : ExecutableRuntimeDependency
{
    /// <inheritdoc />
    public override string DisplayName => "protontricks";
    /// <inheritdoc />
    public override string Description => "Manage Proton games and their dependencies.";
    /// <inheritdoc />
    public override Uri Homepage { get; } = new("https://github.com/Matoking/protontricks");
    /// <inheritdoc />
    public override OSPlatform[] SupportedPlatforms { get; } = [OSPlatform.Linux];
    
    public ProtontricksDependency(IProcessFactory processFactory) : base(processFactory) { }

    protected override Command BuildQueryCommand(PipeTarget outputPipeTarget)
    {
        var command = Cli.Wrap("protontricks").WithArguments("--version").WithStandardOutputPipe(outputPipeTarget);
        return command;
    }

    protected override RuntimeDependencyInformation ToInformation(ReadOnlySpan<char> output)
    {
        if (TryParseVersion(output, out var rawVersion, out var version))
        {
            return new RuntimeDependencyInformation
            {
                RawVersion = rawVersion,
                Version = version,
            };
        }

        return new RuntimeDependencyInformation();
    }

    internal static bool TryParseVersion(ReadOnlySpan<char> output, out string? rawVersion, out Version? version)
    {
        // Example: "protontricks (1.11.1)"
        const string prefix = "protontricks (";
        const string suffix = ")";

        rawVersion = null;
        version = null;

        if (!output.StartsWith(prefix, StringComparison.Ordinal)) return false;
        if (output.Length < prefix.Length + suffix.Length) return false;

        var span = output[prefix.Length..];
        var index = span.IndexOf(suffix);

        if (index == -1) return false;

        span = span[..index];
        rawVersion = span.ToString();
        _ = Version.TryParse(rawVersion, out version);

        return true;
    }

    /// <summary>
    /// Transforms an existing command into a command which invoked `protontricks-launch` with the original command
    /// as the target.
    /// </summary>
    public Command MakeLaunchCommand(Command command, long appId)
    {
        var args = $"--appid {appId} \"{command.TargetFilePath}\" {command.Arguments}";
        return new Command("protontricks-launch", args, 
            command.WorkingDirPath, command.Credentials, command.EnvironmentVariables, 
            command.Validation, command.StandardInputPipe, command.StandardOutputPipe, command.StandardErrorPipe);
    }
}
