namespace NexusMods.Common.UserInput;

/// <summary>
/// The rules used with regards to making a choice among multiple possibilities.
/// </summary>
public enum ChoiceType
{
    /// <summary>
    /// Select 1 from the group.
    /// </summary>
    ExactlyOne,

    /// <summary>
    /// Select 0-1 from the group.
    /// </summary>
    AtMostOne,

    /// <summary>
    /// Select >= 1 from the group.
    /// </summary>
    AtLeastOne,

    /// <summary>
    /// Any number of choices, from 0 to max.
    /// </summary>
    Any,
}
