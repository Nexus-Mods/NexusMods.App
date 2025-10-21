using CliWrap;

namespace NexusMods.Games.Generic.Dependencies;

/// <summary>
/// Common interface for invoking Protontricks across the <see cref="ProtontricksFlatpakDependency"/> and <see cref="ProtontricksNativeDependency"/>.
/// </summary>
public interface IProtontricksDependency
{
    /// <summary>
    /// Transforms an existing command into a command which invokes the Flatpak version of
    /// protontricks-launch with the original command as the target.
    /// </summary>
    public ValueTask<Command> MakeLaunchCommand(Command command, long appId);
}
