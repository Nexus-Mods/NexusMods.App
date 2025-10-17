using CliWrap;
using NexusMods.Sdk;

namespace NexusMods.Games.Generic.Dependencies;

/// <summary>
/// Aggregates multiple Protontricks implementations, using either the native or Flatpak version
/// depending on availability.
/// </summary>
public class AggregateProtontricksDependency : AggregateExecutableRuntimeDependency, IProtontricksDependency
{
    /// <summary>
    /// Creates a new instance of the aggregate Protontricks dependency.
    /// </summary>
    /// <param name="processFactory">The process factory used to execute commands.</param>
    public AggregateProtontricksDependency(IProcessRunner runner) 
        : base(
            displayName: "Protontricks",
            description: "Manage Proton games and their dependencies",
            homepage: new Uri("https://github.com/Matoking/protontricks"),
            dependencies: new ExecutableRuntimeDependency[]
            {
                new ProtontricksNativeDependency(runner),
                new ProtontricksFlatpakDependency(runner),
            })
    {
    }

    /// <inheritdoc />
    public async ValueTask<Command> MakeLaunchCommand(Command command, long appId)
    {
        var availableDependencies = await GetAvailableDependenciesAsync();
        var protontricks = availableDependencies.FirstOrDefault() as IProtontricksDependency;
        if (protontricks == null)
            throw new InvalidOperationException("No Protontricks implementation is available on this system");

        return await protontricks.MakeLaunchCommand(command, appId);
    }

    /// <summary>
    /// Gets whether any Protontricks implementation is available on the system.
    /// </summary>
    public async Task<bool> IsAvailable() => (await QueryInstallationInformation(CancellationToken.None)).HasValue;
}
