namespace NexusMods.DataModel.JsonConverters;

/// <summary>
/// Similar to JsonNameAttribute, but provides additional names for the class
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class JsonAliasAttribute : Attribute
{
    public JsonAliasAttribute(string alias)
    {
        Alias = alias;
    }

    public string Alias { get; }
}
