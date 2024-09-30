using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Telemetry;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Represents a remote mod page on NexusMods.
/// </summary>
[PublicAPI]
public partial class NexusModsModPageMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.NexusModsModPageMetadata";

    /// <summary>
    /// The ID of the mod page.
    /// </summary>
    public static readonly ModIdAttribute ModId = new(Namespace, nameof(ModId)) { IsIndexed = true };

    /// <summary>
    /// The name of the mod page.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));

    /// <summary>
    /// The game of the mod page.
    /// </summary>
    /// <remarks>
    ///     This will be deprecated in the future, in favour of <see cref="GameId"/>.
    ///     The <see cref="GameDomain"/> is a legacy field of the V1 API 
    /// </remarks>
    public static readonly GameDomainAttribute GameDomain = new(Namespace, nameof(GameDomain)) { IsIndexed = true };

    /// <summary>
    /// Unique identifier for the game at the Nexus.
    /// </summary>
    /// <remarks>
    ///     This is the field native to the current (V2) API.
    ///     The <see cref="GameDomain"/> is a legacy field of the V1 API, which will likely be phased out.
    /// </remarks>
    public static readonly GameIdAttribute GameId = new(Namespace, nameof(GameId)) { IsIndexed = true };

    /// <summary>
    /// The last time the mod page was updated (UTC). This is useful for cache invalidation.
    /// </summary>
    /// <remarks>
    ///     Until V2 Update API is done, this will also have to be used for file updates, in the meantime.
    /// </remarks>
    public static readonly DateTimeAttribute UpdatedAt = new(Namespace, nameof(UpdatedAt));
    
    /// <summary>
    /// Uri for the full sized picture of the mod.
    /// </summary>
    public static readonly UriAttribute FullSizedPictureUri = new(Namespace, nameof(FullSizedPictureUri)) { IsOptional = true };

    /// <summary>
    /// Uri for the thumbnail of the full sized picture.
    /// </summary>
    public static readonly UriAttribute ThumbnailUri = new(Namespace, nameof(ThumbnailUri)) { IsOptional = true };

    /// <summary>
    /// Back-reference to all files from this page.
    /// </summary>
    public static readonly BackReferenceAttribute<NexusModsFileMetadata> Files = new(NexusModsFileMetadata.ModPage);

    public partial struct ReadOnly
    {
        public Uri GetUri() => NexusModsUrlBuilder.CreateGenericUri($"https://nexusmods.com/{GameDomain}/mods/{ModId}");
    }
}
