using System.Collections.Immutable;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Sorting.Rules;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// Represents an individual mod recognised by NMA.
/// Please see remarks for current details.
/// </summary>
/// <remarks>
///    At the current moment in time [8th of March 2023]; represents
///    *an installed mod from an archive*, i.e. only archives are supported
///    at the moment and files are pushed out to game directory.<br/><br/>
///
///    This will change some time in the future.
/// </remarks>
[JsonName("NexusMods.DataModel.Mod")]
public record Mod : Entity, IHasEntityId<ModId>
{
    /// <summary>
    /// A unique identifier for this mod within the loadout.
    /// </summary>
    public required ModId Id { get; init; }

    /// <summary>
    /// All files which belong to this mod, accessible by index.
    /// </summary>
    public required EntityDictionary<ModFileId, AModFile> Files { get; init; }

    /// <summary>
    /// Name of the mod in question.
    /// </summary>
    public required string Name { get; init; } = string.Empty;

    /// <summary>
    /// True if the mod is enabled, false otherwise.
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.Loadouts;

    /// <summary>
    /// Defines the individual sorting rules applied to a game.
    /// </summary>
    public ImmutableList<ISortRule<Mod, ModId>> SortRules { get; init; } = ImmutableList<ISortRule<Mod, ModId>>.Empty;
}
