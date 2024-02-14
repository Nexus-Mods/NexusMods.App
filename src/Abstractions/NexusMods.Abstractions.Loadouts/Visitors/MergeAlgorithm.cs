namespace NexusMods.Abstractions.Games.Loadouts.Visitors;

public enum MergeAlgorithm
{
    // Union, In conflicts, A wins
    AOverridesB,
    // Union, In conflicts, B wins
    BOverridesA,
}
