namespace NexusMods.Abstractions.CLI;

/// <summary>
/// A collection of a VerbDefinition, Run delegate, and the type of a Verb used in the CLI.
/// Due to the way type systems work, we must extract some of this information inside .AddVerb().
/// Instead of later in the application when we need to use it, so this is a helper class to store the
/// required information.
/// </summary>
public class RegisteredVerb
{
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
