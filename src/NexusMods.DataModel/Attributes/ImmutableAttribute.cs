namespace NexusMods.DataModel.Attributes;

/// <summary>
/// Used to mark an entity category as immutable. This means we can cache
/// these values, and should not broadcast inserts as no one will be listening.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ImmutableAttribute : Attribute
{

}
