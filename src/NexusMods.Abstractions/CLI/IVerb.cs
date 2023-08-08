namespace NexusMods.Abstractions.CLI;

/// <summary>
/// Represents an individual action, e.g. 'Analyze Game'
/// </summary>
public interface IVerb
{
    /// <summary>
    /// The function that is ran to execute it.
    /// </summary>
    public Delegate Delegate { get; }

    /// <summary>
    /// Describes the verb; its name, description and options.
    /// </summary>
    static abstract VerbDefinition Definition { get; }
}
