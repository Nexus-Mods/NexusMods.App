using JetBrains.Annotations;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// A definition of an option, used as an abstraction layer between the CLI handler code and the verb code.
/// </summary>
[PublicAPI]
public record OptionDefinition(Type Type, string ShortName, string LongName, string HelpText, bool IsInjected, bool IsOptional);
