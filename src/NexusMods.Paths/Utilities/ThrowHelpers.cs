namespace NexusMods.Paths.Utilities;

/// <summary>
/// Class with helper methods for throwing exceptions from our code.
/// </summary>
/// <remarks>
///    Exceptions prevent the JIT from being able to inline functions;
///    as such, for small method bodies it may be sometimes preferable
///    (from both a size and performance perspective) to delegate the
///    throwing of exceptions to a separate class.
/// </remarks>
public class ThrowHelpers
{
    /// <summary>
    /// Throws <see cref="Utilities.PathException"/>(s).
    /// </summary>
    /// <param name="message">The message to throw.</param>
    public static void PathException(string message) => throw new PathException(message);
}

/// <inheritdoc />
public class PathException : Exception
{
    /// <inheritdoc />
    public PathException(string ex) : base(ex) { }
}