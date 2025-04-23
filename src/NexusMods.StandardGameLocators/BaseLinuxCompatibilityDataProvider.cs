using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Base implementation of <see cref="ILinuxCompatibilityDataProvider"/>.
/// </summary>
public class BaseLinuxCompatibilityDataProvider : ILinuxCompatibilityDataProvider
{
    /// <inheritdoc/>
    public AbsolutePath WinePrefixDirectoryPath { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public BaseLinuxCompatibilityDataProvider(AbsolutePath winePrefixDirectoryPath)
    {
        WinePrefixDirectoryPath = winePrefixDirectoryPath;
    }

    /// <inheritdoc/>
    public virtual ValueTask<WineDllOverride[]> GetWineDllOverrides(CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<WineDllOverride[]>([]);
    }

    /// <inheritdoc/>
    public virtual async ValueTask<IReadOnlySet<string>> GetInstalledWinetricksComponents(CancellationToken cancellationToken)
    {
        var winetricksFilePath = WinePrefixDirectoryPath.Combine("winetricks.log");
        if (!winetricksFilePath.FileExists) return new HashSet<string>();

        await using var stream = winetricksFilePath.Read();
        var result = WineParser.ParseWinetricksLogFile(stream);

        return result;
    }
}
