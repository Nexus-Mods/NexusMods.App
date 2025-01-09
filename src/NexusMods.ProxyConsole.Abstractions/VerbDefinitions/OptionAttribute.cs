using System;

namespace NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

/// <summary>
/// Marks a Parameter as a CLI option.
/// </summary>
/// <param name="shortName"></param>
/// <param name="longName"></param>
/// <param name="helpText"></param>
[AttributeUsage(AttributeTargets.Parameter)]
public class OptionAttribute(string shortName, string longName, string helpText, bool isOptional = false) : Attribute
{
    /// <summary>
    /// The short name of the option. For example `h`
    /// </summary>
    public string ShortName { get; } = shortName;

    /// <summary>
    /// The long name of the option. For example `help`
    /// </summary>
    public string LongName { get; } = longName;

    /// <summary>
    /// The help text for the option.
    /// </summary>
    public string HelpText { get; } = helpText;
    
    /// <summary>
    /// True if the option is optional, false otherwise.
    /// </summary>
    public bool IsOptional { get; } = isOptional;
}
