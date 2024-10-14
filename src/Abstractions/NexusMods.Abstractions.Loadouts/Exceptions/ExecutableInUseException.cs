using System.Runtime.Serialization;
namespace NexusMods.Abstractions.Loadouts.Exceptions;

/// <summary>
/// Exception thrown when an executable is in use
/// </summary>
public class ExecutableInUseException : Exception
{
    /// <inheritdoc />
    public ExecutableInUseException() { }
    /// <inheritdoc />
    public ExecutableInUseException(string? message) : base(message) { }
    /// <inheritdoc />
    public ExecutableInUseException(string? message, Exception? innerException) : base(message, innerException) { }
}
