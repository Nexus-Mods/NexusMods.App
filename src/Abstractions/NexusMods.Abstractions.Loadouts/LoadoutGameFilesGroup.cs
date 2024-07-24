using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Represents a group of game file loadout items.
/// </summary>
[PublicAPI]
[Include<LoadoutItemGroup>]
public partial class LoadoutGameFilesGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.LoadoutGameFilesGroup";

    /// <summary>
    /// Raw location id.
    /// </summary>
    public static readonly StringAttribute RawLocationId = new(Namespace, nameof(LocationId)) { IsIndexed = true };

    /// <summary/>
    [PublicAPI]
    public partial struct ReadOnly
    {
        /// <summary>
        /// Gets the location ID.
        /// </summary>
        public LocationId LocationId => LocationId.From(RawLocationId);

        /// <summary>
        /// Gets the children as <see cref="LoadoutFile.ReadOnly"/>.
        /// </summary>
        public IEnumerable<LoadoutFile.ReadOnly> GameFiles => AsLoadoutItemGroup().Children.OfTypeLoadoutItemWithTargetPath().OfTypeLoadoutFile();
    }
}


