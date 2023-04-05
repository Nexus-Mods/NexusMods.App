namespace NexusMods.Common.UserInput;

/// <summary>
/// Describes the state of a selectable option.
/// </summary>
public enum OptionState
{
    /// <summary>
    /// Not selected but could be.
    /// </summary>
    Available,

    /// <summary>
    /// Selected, could be deselected.
    /// </summary>
    Selected,

    /// <summary>
    /// Not selected and can't be.
    /// </summary>
    Disabled,

    /// <summary>
    /// Selected and can't be deselected.
    /// </summary>
    Required,
}
