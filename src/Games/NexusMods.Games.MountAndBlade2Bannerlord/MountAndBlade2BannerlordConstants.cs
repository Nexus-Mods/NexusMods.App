using Bannerlord.LauncherManager;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public static class MountAndBlade2BannerlordConstants
{
    public static readonly string DocumentsFolderName = "Mount and Blade II Bannerlord";

    public static readonly RelativePath ModFolder = Constants.ModulesFolder.ToRelativePath();
    public static readonly RelativePath SubModuleFile = Constants.SubModuleName.ToRelativePath();
}
