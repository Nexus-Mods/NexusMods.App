using JetBrains.Annotations;
using NexusMods.Paths;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Linux compatibility data provider.
/// </summary>
[PublicAPI]
public interface ILinuxCompatibilityDataProvider
{
    /// <summary>
    /// Path to the WINE prefix directory.
    /// </summary>
    public AbsolutePath WinePrefixDirectoryPath { get; }

    /// <summary>
    /// Gets all WINE DLL overrides.
    /// </summary>
    public ValueTask<WineDllOverride[]> GetWineDllOverrides(CancellationToken cancellationToken);

    /// <summary>
    /// Gets all installed winetricks components.
    /// </summary>
    public ValueTask<IReadOnlySet<string>> GetInstalledWinetricksComponents(CancellationToken cancellationToken);
}
