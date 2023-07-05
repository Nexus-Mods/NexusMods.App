using JetBrains.Annotations;
using Vogen;

namespace NexusMods.DataModel.Diagnostics;

// TODO: figure out the formatting of this message, so that certain elements can be made clickable
// example: "Mod '{{modName}}' is broken, please fix!" where {{modName}} should be clickable

/// <summary>
/// Represents a message of a diagnostic.
/// </summary>
[PublicAPI]
[ValueObject<string>]
public readonly partial struct DiagnosticMessage
{
    private static Validation Validate(string input)
    {
        // TODO: check formatting
        return Validation.Ok;
    }
}
