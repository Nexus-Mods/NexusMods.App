using System;

namespace NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

/// <summary>
/// Defines a method that should be exposed as a verb.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class VerbAttribute : Attribute
{
    /// <summary>
    /// Defines a method that should be exposed as a verb.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    public VerbAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
    
    /// <summary>
    /// The name of the verb.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Help text for the verb.
    /// </summary>
    public string Description { get; }
}
