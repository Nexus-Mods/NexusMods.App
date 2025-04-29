using TransparentValueObjects;

namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// Unique identifier for a given site user.
/// </summary>
[ValueObject<string>]
// ReSharper disable once InconsistentNaming
public readonly partial struct NXMKey { }
