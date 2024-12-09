using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Paths;
using static Bannerlord.LauncherManager.Constants;
namespace NexusMods.Games.MountAndBlade2Bannerlord;

/// <summary>
/// Bannerlord specific extensions for loadouts.
/// </summary>
public static class LoadoutExtensions
{
    public static readonly RelativePath BlseExecutable = (RelativePath)"Bannerlord.BLSE.Standalone.exe";
    
    /// <summary>
    /// Determines if BLSE is installed by matching a file name within a given loadout.
    /// </summary>
    public static bool LocateBLSE(this Loadout.ReadOnly loadout, out RelativePath path)
    {
        var blseXboxPath = (RelativePath)BinFolder / XboxConfiguration / BlseExecutable;
        var blseStandalonePath = (RelativePath)BinFolder / Win64Configuration / BlseExecutable;
        var blseLauncher = loadout.Items.OfTypeLoadoutItemWithTargetPath()
            .Where(x => x.AsLoadoutItem().IsEnabled())
            .FirstOrDefault(x =>
            {
                var relativePath = x.TargetPath.Item3;
                return relativePath.Equals(blseXboxPath) || relativePath.Equals(blseStandalonePath);
            }
        );

        if (!blseLauncher.IsValid())
        {
            path = default(RelativePath);
            return false;
        }

        path = blseLauncher.TargetPath.Item3;
        return true;
    }

    /// <summary>
    /// Determines if BLSE is installed in a given loadout by matching its launcher name.
    /// </summary>
    public static bool IsBLSEInstalled(this Loadout.ReadOnly loadout) => LocateBLSE(loadout, out _);
}
