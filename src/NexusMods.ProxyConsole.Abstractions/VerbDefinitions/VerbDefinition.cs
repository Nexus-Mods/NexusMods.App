using System.Reflection;

namespace NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

/// <summary>
/// A definition of a verb, used as an abstraction layer between the CLI handler code and the verb code.
/// </summary>
/// <param name="Name">The name of the verb, prefixed by the module path it is in</param>
/// <param name="Description">A description of the verb</param>
/// <param name="Info">The method to invoke to run the verb</param>
/// <param name="Options">Option definitions for the method's parameters</param>
public record VerbDefinition(string Name, string Description, MethodInfo Info, OptionDefinition[] Options);
