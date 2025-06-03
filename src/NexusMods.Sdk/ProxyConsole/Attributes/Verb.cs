using JetBrains.Annotations;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// Marks the method to be exposed as CLI verb.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[PublicAPI]
public class VerbAttribute(string name, string description) : Attribute
{
    /// <summary>
    /// The name of the verb.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Help text for the verb.
    /// </summary>
    public string Description { get; } = description;
}
