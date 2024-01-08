namespace NexusMods.DataModel.JsonConverters;

/// <summary>
/// Used to mark individual types which are injected during serialization/deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class JsonInjectedAttribute : Attribute { }
