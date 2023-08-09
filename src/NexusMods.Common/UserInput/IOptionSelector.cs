namespace NexusMods.Common.UserInput;

/// <summary>
/// An interface for selecting item(s) from a group.
/// </summary>
/// <remarks>
///     This implementation is inspired by FOMOD but has been slightly generalised out;
///     so theoretically could be reused.
/// </remarks>
public interface IOptionSelector
{
    /// <summary>
    /// Requests the user to make a choice from an individual group of choices.
    /// </summary>
    /// <param name="query">The prompt displayed to the user for this choice.</param>
    /// <param name="type">The kind of choice the user has to make.</param>
    /// <param name="options">The items the user can select from.</param>
    /// <typeparam name="TOptionId">Unique ID for each option.</typeparam>
    /// <returns>Number of picked options by the user.</returns>
    public Task<TOptionId[]> RequestChoice<TOptionId>(string query, ChoiceType type, Option<TOptionId>[] options);

    /// <summary>
    /// Requests the user to make a series of choices from multiple groups of choices.
    /// </summary>
    /// <param name="choices">Number of individual groups the user will make choices from.</param>
    /// <typeparam name="TGroupId">Unique ID for each group.</typeparam>
    /// <typeparam name="TOptionId">Unique ID for each option.</typeparam>
    /// <returns></returns>
    public Task<Tuple<TGroupId, TOptionId[]>?> RequestMultipleChoices<TGroupId, TOptionId>(ChoiceGroup<TGroupId, TOptionId>[] choices);
}
