using TransparentValueObjects;

namespace NexusMods.Abstractions.Activities;

/// <summary>
/// A unique identifier for an activity.
/// </summary>
[ValueObject<Guid>]
[Obsolete(message: "To be replaced with Jobs")]
public readonly partial struct ActivityId;
