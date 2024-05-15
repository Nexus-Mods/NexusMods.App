using System;

namespace NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

/// <summary>
/// Marks a parameter as an injected dependency.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class InjectedAttribute : Attribute
{

}
