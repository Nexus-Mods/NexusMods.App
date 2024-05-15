using System.Reflection;

namespace NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

/// <summary>
/// A definition of a verb, used as an abstraction layer between the CLI handler code and the verb code.
/// </summary>
/// <param name="Name"></param>
/// <param name="Description"></param>
/// <param name="Info"></param>
/// <param name="Options"></param>
public record VerbDefinition(string Name, string Description, MethodInfo Info, OptionDefinition[] Options);
