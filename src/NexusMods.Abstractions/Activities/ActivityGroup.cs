using TransparentValueObjects;

namespace NexusMods.Abstractions.Activities;

/// <summary>
/// A name for a group of activities, groups are useful for filtering and sorting activities. For example
/// a group could be "Download" or "Mod Install".
/// </summary>
[ValueObject<string>]
public partial struct ActivityGroup;
