using NexusMods.DataModel.Loadouts.Mods;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// A helper class for modifying specific parts of a loadout.
/// </summary>
public class ALoadoutVisitor
{
    /// <summary>
    /// Override this method to modify a mod. Return null to remove the mod from the loadout.
    /// </summary>
    /// <param name="mod"></param>
    /// <returns></returns>
    public virtual Mod? Alter(Mod mod)
    {
        return mod;
    }

    /// <summary>
    /// Override this method to modify a mod file. Return null to remove the file from the loadout.
    /// </summary>
    /// <param name="modFile"></param>
    /// <returns></returns>
    public virtual AModFile? Alter(AModFile modFile)
    {
        return modFile;
    }

    /// <summary>
    /// Override this method to modify a loadout.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public virtual Loadout Alter(Loadout loadout)
    {
        return loadout;
    }

}
