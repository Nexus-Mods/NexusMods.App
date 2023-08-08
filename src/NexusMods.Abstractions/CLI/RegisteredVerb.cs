namespace NexusMods.Abstractions.CLI;

/// <summary>
/// Represents an individual action, e.g. 'Analyze Game'
/// </summary>
public class RegisteredVerb
{
    /// <summary>
    /// Renderer to use for this verb, will be set by the CLI .
    /// </summary>
    public IRenderer Renderer { get; set; } = null!;
    /// <summary>
    /// Describes the verb; its name, description and options.
    /// </summary>
    public required VerbDefinition Definition { get; init; }

    /// <summary>
    /// The function that is ran to execute it.
    /// </summary>
    public required Func<object, Delegate> Run { get; init; }

    /// <summary>
    /// Generic type of the class used.
    /// </summary>
    public required Type Type { get; init; }
}
