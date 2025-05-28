using JetBrains.Annotations;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// Marks a Parameter as a CLI option.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[PublicAPI]
public class OptionAttribute(string shortName, string longName, string helpText, bool isOptional = false) : Attribute
{
    /// <summary>
    /// The short name of the option.
    /// </summary>
    /// <example>
    /// h
    /// </example>
    public string ShortName { get; } = shortName;

    /// <summary>
    /// The long name of the option.
    /// </summary>
    /// <example>
    /// help
    /// </example>
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

