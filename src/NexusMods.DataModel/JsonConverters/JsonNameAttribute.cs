namespace NexusMods.DataModel.JsonConverters;

/// <summary>
/// Marks a class for JsonConverter<> generation by DataModel.CodeGenerator class.
/// Types marked with this attribute participate in the polymorphic deserialization
/// features of the project and their data is marked by the $type: "name" field on each
/// JSON object
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class JsonNameAttribute : Attribute
{
    public JsonNameAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}