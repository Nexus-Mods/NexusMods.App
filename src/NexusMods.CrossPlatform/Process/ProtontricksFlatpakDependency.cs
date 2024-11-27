using System.Runtime.InteropServices;
using CliWrap;
namespace NexusMods.CrossPlatform.Process;

/// <summary>
///     Protontricks is a tool that helps manage Proton games and their dependencies.
///     This implementation specifically targets the Flatpak version of Protontricks,
///     commonly found on Steam Deck (SteamOS) and other Linux distributions.
/// </summary>
public class ProtontricksFlatpakDependency : ExecutableRuntimeDependency, IProtontricksDependency
{
    private const string FlatpakPackageId = "com.github.Matoking.protontricks";

    /// <inheritdoc />
    public override string DisplayName => "protontricks (flatpak)";
    /// <inheritdoc />
    public override string Description => "Manage Proton games and their dependencies (Flatpak version)";
    /// <inheritdoc />
    public override Uri Homepage { get; } = new("https://github.com/Matoking/protontricks");
    /// <inheritdoc />
    public override OSPlatform[] SupportedPlatforms { get; } = [OSPlatform.Linux];

    /// <inheritdoc />
    public ProtontricksFlatpakDependency(IProcessFactory processFactory) : base(processFactory) { }

    /// <inheritdoc />
    protected override RuntimeDependencyInformation ToInformation(ReadOnlySpan<char> output) => ProtontricksNativeDependency.ToInformationImpl(output);

    /// <inheritdoc />
    protected override Command BuildQueryCommand(PipeTarget outputPipeTarget)
    {
        var command = Cli.Wrap("flatpak")
            .WithArguments($"run {FlatpakPackageId} --version")
            .WithStandardOutputPipe(outputPipeTarget);
        return command;
    }

    /// <summary>
    /// Transforms an existing command into a command which invokes the Flatpak version of
    /// protontricks-launch with the original command as the target.
    /// </summary>
    public ValueTask<Command> MakeLaunchCommand(Command command, long appId)
    {
        var args = $"run --command=protontricks-launch {FlatpakPackageId} --appid {appId} \"{command.TargetFilePath}\" {command.Arguments}";
        return ValueTask.FromResult(new Command("flatpak", args,
            command.WorkingDirPath, command.Credentials, command.EnvironmentVariables,
            command.Validation, command.StandardInputPipe, command.StandardOutputPipe, command.StandardErrorPipe));
    }

    /// <summary>
    /// Checks if the Protontricks Flatpak is installed on the system.
    /// </summary>
    /// <returns>True if the Flatpak is installed, false otherwise.</returns>
    public async Task<bool> IsFlatpakInstalledAsync()
    {
        try
        {
            var result = await Cli.Wrap("flatpak")
                .WithArguments($"info {FlatpakPackageId}")
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();
            
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
