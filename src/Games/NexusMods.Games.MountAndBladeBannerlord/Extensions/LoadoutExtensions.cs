using NexusMods.DataModel.Loadouts;
using NexusMods.Games.MountAndBladeBannerlord.Loadouts;

namespace NexusMods.Games.MountAndBladeBannerlord.Extensions;

public static class LoadoutExtensions
{
    public static bool HasModuleId(this Loadout loadout, string moduleId)
    {
       return loadout.Mods.Any(x => x.Value.Files.FirstOrDefault().Value.Metadata.OfType<ModuleIdMetadata>().FirstOrDefault()?.ModuleId == moduleId);
    }
}