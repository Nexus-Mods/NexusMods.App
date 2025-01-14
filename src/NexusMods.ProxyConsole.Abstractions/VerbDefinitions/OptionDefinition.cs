using System;

namespace NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

/// <summary>
/// A definition of an option, used as an abstraction layer between the CLI handler code and the verb code.
/// </summary>
/// <param name="Type"></param>
/// <param name="ShortName"></param>
/// <param name="LongName"></param>
/// <param name="HelpText"></param>
/// <param name="IsInjected"></param>
public record OptionDefinition(Type Type, string ShortName, string LongName, string HelpText, bool IsInjected, bool IsOptional);
