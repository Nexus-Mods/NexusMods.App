namespace NexusMods.Abstractions.UI;

/// <summary>
/// Static method for throwing exceptions on given conditions with minimal overhead.
/// </summary>
/// <remarks>
///     Throwing exceptions prevents the inlining of functions.
///     Check for the exceptions that you need to throw first, then use these methods to throw them,
///     as exceptions, given their name, are 'exceptional' by nature. They are on cold path.
/// </remarks>
public static class ThrowHelpers
{
    /// <summary>
    /// Throws an <see cref="System.ArgumentNullException"/> with a given property name.
    /// </summary>
    /// <param name="propertyName">Name of the property to throw the exception for.</param>
    public static void ArgumentNullException(string propertyName) => throw new ArgumentNullException(propertyName);
}
