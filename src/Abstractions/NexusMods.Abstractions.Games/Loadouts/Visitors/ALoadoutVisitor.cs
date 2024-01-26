using NexusMods.Abstractions.DataModel.Entities.Mods;

namespace NexusMods.Abstractions.Games.Loadouts.Visitors;

/// <summary>
/// A helper class for modifying specific parts of a loadout.
/// </summary>
public class ALoadoutVisitor
{
    /// <summary>
    /// Transforms the loadout using the visitor pattern.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public Loadout Transform(Loadout loadout)
    {
        loadout = AlterBefore(loadout);
        // Callback hell? Never heard of it!
        return AlterAfter(loadout with
        {
            Mods = loadout.Mods.Keep(mod =>
            {
                var modTmp = AlterBefore(loadout, mod);
                if (modTmp is null) return null;
                mod = modTmp;

                return AlterAfter(loadout, mod with
                {
                    Files = mod.Files.Keep(modFile =>
                    {
                        var modFileTmp = AlterBefore(loadout, mod, modFile);
                        if (modFileTmp is null) return null;
                        modFile = modFileTmp;
                        return AlterAfter(loadout, mod, modFile);
                    })
                });
            })
        });
    }


    /// <summary>
    /// Override this method to modify a mod before any children are modified. Return null to remove the mod from the loadout.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="mod"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    protected virtual Mod? AlterBefore(Loadout loadout, Mod mod)
    {
        return mod;
    }

    /// <summary>
    /// Override this method to modify a loadout before any children are visited.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    protected virtual Loadout AlterBefore(Loadout loadout)
    {
        return loadout;
    }

    /// <summary>
    /// Override this method to modify a mod file before any children are visited.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="mod"></param>
    /// <param name="modFile"></param>
    /// <returns></returns>
    protected virtual AModFile? AlterBefore(Loadout loadout, Mod mod, AModFile modFile)
    {
        return modFile;
    }

    /// <summary>
    /// Override this method to modify a mod after children have been visited. Return null to remove the mod from the loadout.
    /// </summary>
    /// <param name="mod"></param>
    /// <returns></returns>
    protected virtual Mod? AlterAfter(Loadout loadout, Mod mod)
    {
        return mod;
    }

    /// <summary>
    /// Override this method to modify a mod file after any children have been visited. Return null to remove the file from the loadout.
    /// </summary>
    /// <param name="mod"></param>
    /// <param name="modFile"></param>
    /// <param name="loadout"></param>
    /// <returns></returns>
    protected virtual AModFile? AlterAfter(Loadout loadout, Mod mod, AModFile modFile)
    {
        return modFile;
    }

    /// <summary>
    /// Override this method to modify a loadout after any children have been visited.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    protected virtual Loadout AlterAfter(Loadout loadout)
    {
        return loadout;
    }

}
