using Bannerlord.ModuleManager;
using FetchBannerlordVersion;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;

partial class LauncherManagerNexusModsApp
{
    public override string GetGameVersion()
    {
        var gamePath = GetInstallPath();
        var versionStr = Fetcher.GetVersion(gamePath, "TaleWorlds.Library.dll");
        return ApplicationVersion.TryParse(versionStr, out var av) ? $"{av.Major}.{av.Minor}.{av.Revision}.{av.ChangeSet}" : "0.0.0.0";
    }

    public override int GetChangeset()
    {
        var gamePath = GetInstallPath();
        return Fetcher.GetChangeSet(gamePath, "TaleWorlds.Library.dll");
    }
}
