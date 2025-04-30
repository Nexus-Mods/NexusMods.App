namespace NexusMods.Abstractions.Serialization.Attributes;

/// <summary>
/// Similar to <see cref="JsonNameAttribute"/>, but provides additional names for the class
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class JsonAliasAttribute : Attribute
{
    /// <summary>
    /// The alias for the given JSON name
    /// </summary>
    public string Alias { get; }

    /// <summary>
    /// Creates an alias for a given <see cref="JsonNameAttribute"/>.
    /// </summary>
    /// <param name="alias">Alias for the <see cref="JsonNameAttribute"/></param>
    public JsonAliasAttribute(string alias)
    {
        Alias = alias;
    }
}
