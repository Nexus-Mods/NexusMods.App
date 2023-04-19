using GameFinder.Common;
using GameFinder.StoreHandlers.EADesktop;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Finds games managed by the 'EA Desktop' application, the successor to Origin.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EADesktopLocator : AGameLocator<EADesktopGame, string, IEADesktopGame, EADesktopLocator>
{
    /// <summary/>
    public EADesktopLocator(IServiceProvider provider) : base(provider)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<string> Ids(IEADesktopGame game) => game.EADesktopSoftwareIDs;

    /// <inheritdoc />
    protected override AbsolutePath Path(EADesktopGame record) => record.BaseInstallPath.ToAbsolutePath(FileSystem);
}
