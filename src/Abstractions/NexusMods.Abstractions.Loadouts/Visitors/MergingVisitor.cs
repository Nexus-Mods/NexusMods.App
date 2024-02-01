using NexusMods.Abstractions.Games.Loadouts.Visitors;
using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.Abstractions.Loadouts.Visitors;

/// <summary>
/// A visitor that merges two loadouts together, at each level of the tree the merge method is called,
/// so you can override the merge behaviour at each level. By default the merge algorithm is BOverridesA.
/// </summary>
public class MergingVisitor
{
    private readonly MergeAlgorithm _fileMergeAlgorithm;

    public MergingVisitor(MergeAlgorithm fileMergeAlgorithm = MergeAlgorithm.BOverridesA)
    {
        _fileMergeAlgorithm = fileMergeAlgorithm;
    }


    /// <summary>
    /// Merges two loadouts together.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public Loadout Transform(Loadout a, Loadout b)
    {
        return Merge(a, b);
    }

    /// <summary>
    /// Merges two loadouts together.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    protected virtual Loadout Merge(Loadout a, Loadout b)
    {
        return a with { Mods = a.Mods.Merge(b.Mods, Merge) };
    }


    /// <summary>
    /// Merges two mods together.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    protected virtual Mod? Merge(Mod? a, Mod? b)
    {
        if (a is null && b is null)
            return null;
        if (a is null)
            return b;
        if (b is null)
            return a;

        return a with { Files = a.Files.Merge(b.Files, Merge) };
    }

    /// <summary>
    /// Merges two mod files together.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    protected virtual AModFile? Merge(AModFile? a, AModFile? b)
    {
        return _fileMergeAlgorithm switch
        {
            MergeAlgorithm.AOverridesB => a,
            MergeAlgorithm.BOverridesA => b,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
