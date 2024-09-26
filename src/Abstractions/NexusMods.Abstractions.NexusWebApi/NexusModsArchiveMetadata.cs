﻿using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusWebApi;

/// <summary>
/// Archive metadata for a download that was installed from a NexusMods mod.
/// </summary>
public partial class NexusModsArchiveMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.NexusWebApi.NexusModsArchiveMetadata";
    
    /// <summary>
    /// The NexusMods API game ID.
    /// </summary>
    public static readonly StringAttribute GameId = new(Namespace, nameof(GameId)) {IsIndexed = true};

    /// <summary>
    /// Mod ID corresponding to the Nexus API.
    /// </summary>
    public static readonly ModIdAttribute ModId = new(Namespace, nameof(ModId)) { IsIndexed = true };

    /// <summary>
    /// File ID corresponding to the Nexus API.
    /// </summary>
    public static readonly FileIdAttribute FileId = new(Namespace, nameof(FileId)) { IsIndexed = true };
}
