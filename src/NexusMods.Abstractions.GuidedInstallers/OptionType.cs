namespace NexusMods.Abstractions.GuidedInstallers;

/// <summary>
/// Describes the type of an option.
/// </summary>
public enum OptionType
{
    /// <summary>
    /// The option can be selected.
    /// </summary>
    Available,

    /// <summary>
    /// The option can't be selected.
    /// </summary>
    Disabled,

    /// <summary>
    /// This option is selected by default.
    /// </summary>
    PreSelected,

    /// <summary>
    /// The option is always selected.
    /// </summary>
    Required,
}
