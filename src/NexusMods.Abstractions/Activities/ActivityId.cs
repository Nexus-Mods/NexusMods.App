using TransparentValueObjects;

namespace NexusMods.Abstractions.Activities;

/// <summary>
/// A unique identifier for an activity.
/// </summary>
[ValueObject<Guid>]
public readonly partial struct ActivityId
{

}
