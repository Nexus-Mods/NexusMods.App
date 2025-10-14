using System.Runtime.InteropServices;
using DynamicData.Kernel;
using NexusMods.Paths;

namespace NexusMods.Sdk;

/// <summary>
/// Represents a runtime dependency that aggregates multiple executable runtime dependencies,
/// using the first available implementation.
/// </summary>
public class AggregateExecutableRuntimeDependency : IRuntimeDependency
{
    private readonly ExecutableRuntimeDependency[] _dependencies;
    private ExecutableRuntimeDependency? _activeDependency;

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public Uri Homepage { get; }

    /// <inheritdoc />
    public OSPlatform[] SupportedPlatforms { get; }

    /// <inheritdoc />
    public RuntimeDependencyType DependencyType => RuntimeDependencyType.Executable;
    
    /// <summary>
    /// Creates a new aggregate runtime dependency.
    /// </summary>
    /// <param name="displayName">Display name for the aggregate dependency</param>
    /// <param name="description">Description of the functionality provided</param>
    /// <param name="homepage">Homepage URL for the software</param>
    /// <param name="dependencies">Array of alternative implementations that can satisfy this dependency</param>
    public AggregateExecutableRuntimeDependency(
        string displayName,
        string description,
        Uri homepage,
        ExecutableRuntimeDependency[] dependencies)
    {
        if (dependencies.Length == 0)
            throw new ArgumentException("At least one dependency must be provided", nameof(dependencies));

        _dependencies = dependencies;
        DisplayName = displayName;
        Description = description;
        Homepage = homepage;
        SupportedPlatforms = dependencies
            .SelectMany(d => d.SupportedPlatforms)
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Gets the currently active dependency implementation that is both installed and supports the current operating system.
    /// </summary>
    private async ValueTask<ExecutableRuntimeDependency?> GetActiveDependencyAsync(CancellationToken cancellationToken = default)
    {
        if (_activeDependency != null)
            return _activeDependency;

        foreach (var dependency in _dependencies)
        {
            try
            {
                // Verify the inner dependency supports our OS.
                if (!dependency.SupportedPlatforms.Contains(OSInformation.Shared.Platform))
                    continue;

                var result = await dependency.QueryInstallationInformation(cancellationToken);
                if (!result.HasValue) 
                    continue;

                _activeDependency = dependency;
                return dependency;
            }
            catch
            {
                // ignored
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async ValueTask<Optional<RuntimeDependencyInformation>> QueryInstallationInformation(CancellationToken cancellationToken)
    {
        var active = await GetActiveDependencyAsync(cancellationToken);
        if (active == null)
            return Optional<RuntimeDependencyInformation>.None;

        return await active.QueryInstallationInformation(cancellationToken);
    }

    /// <summary>
    /// Gets all available dependencies that are currently installed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async ValueTask<IReadOnlyList<ExecutableRuntimeDependency>> GetAvailableDependenciesAsync(
        CancellationToken cancellationToken = default)
    {
        var available = new List<ExecutableRuntimeDependency>();

        foreach (var dependency in _dependencies)
        {
            try
            {
                var result = await dependency.QueryInstallationInformation(cancellationToken);
                if (result.HasValue)
                    available.Add(dependency);
            }
            catch
            {
                // ignored
            }
        }

        return available;
    }
}
