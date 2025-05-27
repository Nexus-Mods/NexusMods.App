using System.Reflection;
using JetBrains.Annotations;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// A definition of a verb, used as an abstraction layer between the CLI handler code and the verb code.
/// </summary>
[PublicAPI]
public record VerbDefinition(string Name, string Description, MethodInfo Info, OptionDefinition[] Options);
