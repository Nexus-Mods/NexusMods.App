using System;

namespace NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

/// <summary>
/// Defines a method that should be exposed as a verb.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
[AttributeUsage(AttributeTargets.Method)]
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
