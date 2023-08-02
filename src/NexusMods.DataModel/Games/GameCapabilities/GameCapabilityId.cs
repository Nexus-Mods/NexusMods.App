using JetBrains.Annotations;
using Vogen;

namespace NexusMods.DataModel.Games.GameCapabilities;

/// <summary>
/// Represents a unique identifier for a game capability type.
/// </summary>
[PublicAPI]
[ValueObject<Guid>(conversions: Conversions.None)]
public readonly partial struct GameCapabilityId { }
