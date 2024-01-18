using System.Collections.Immutable;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;

namespace NexusMods.Abstractions.DataModel.Entities.Mods;

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
    /// Category used for 'Game Files'.
    /// </summary>
    public const string GameFilesCategory = "Game Files";

    /// <summary>
    /// Category used for 'Preferences'.
    /// </summary>
    public const string PreferencesCategory = "Preferences";

    /// <summary>
    /// Category used for 'Save Games'.
    /// </summary>
    public const string SavesCategory = "Saved Games";

    /// <summary>
    /// Category used for 'Overrides'.
    /// </summary>
    public const string OverridesCategory = "Overrides";

    /// <summary>
    /// A category used for 'Modding Metadata', that is files that contain metadata about mods, like plugins.txt files
    /// for Bethesda games, or redmod generated files for Cyberpunk 2077.
    /// </summary>
    public const string ModdingMetaData = "Modding Metadata";

    /// <summary>
    /// A unique identifier for this mod within the loadout.
    /// </summary>
    public required ModId Id { get; init; }

    /// <summary>
    /// All files which belong to this mod, accessible by index.
    /// </summary>
    public required EntityDictionary<ModFileId, AModFile> Files { get; init; }

    /// <summary>
    /// Metadata of the mod.
    /// </summary>
    public AModMetadata? Metadata { get; set; }

    /// <summary>
    /// Category of the mod
    /// </summary>
    public string ModCategory { get; set; } = string.Empty;

    /// <summary>
    /// Version of the mod
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Name of the mod in question.
    /// </summary>
    public required string Name { get; init; } = string.Empty;

    /// <summary>
    /// True if the mod is enabled, false otherwise.
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.Mods;

    /// <summary>
    /// Defines the individual sorting rules applied to a game.
    /// </summary>
    public ImmutableList<ISortRule<Mod, ModId>> SortRules { get; init; } = ImmutableList<ISortRule<Mod, ModId>>.Empty;

    /// <summary>
    /// The install status of the mod.
    /// </summary>
    public ModStatus Status { get; init; } = ModStatus.Installed;

    /// <summary>
    /// Date and time when the mod was installed.
    /// </summary>
    public DateTime Installed { get; set; } = DateTime.UtcNow;
}
