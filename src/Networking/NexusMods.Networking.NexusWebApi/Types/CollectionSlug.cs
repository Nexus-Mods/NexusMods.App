using TransparentValueObjects;

namespace NexusMods.Networking.NexusWebApi.Types;

/// <summary>
/// Unique identifier for a collection hosted on Nexus.
/// </summary>
[ValueObject<string>]
public readonly partial struct CollectionSlug { }
