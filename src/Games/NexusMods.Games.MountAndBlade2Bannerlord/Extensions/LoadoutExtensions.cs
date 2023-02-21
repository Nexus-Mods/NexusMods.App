using NexusMods.DataModel.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.Loadouts;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Extensions;

public static class LoadoutExtensions
{
    public static bool HasModuleId(this Loadout loadout, string moduleId)
    {
       return loadout.Mods.Any(x => x.Value.Files.FirstOrDefault().Value.Metadata.OfType<ModuleIdMetadata>().FirstOrDefault()?.ModuleId == moduleId);
    }
}