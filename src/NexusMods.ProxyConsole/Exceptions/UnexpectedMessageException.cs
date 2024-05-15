using System;

namespace NexusMods.ProxyConsole.Exceptions;

/// <summary>
/// An exception that is thrown when an unexpected message is received, most often happens during the single threaded
/// RPC process of the serializer.
/// </summary>
public class UnexpectedMessageException : Exception
{
    private UnexpectedMessageException(Type expectedType, Type actualType) :
        base($"Expected message of type {expectedType.Name}, but received message of type {actualType.Name}")
    {
    }

    /// <summary>
    /// Creates a new <see cref="UnexpectedMessageException"/> instance and throws it.
    /// </summary>
    /// <param name="expectedType"></param>
    /// <param name="actualType"></param>
    /// <exception cref="UnexpectedMessageException"></exception>
    public static void Throw(Type expectedType, Type actualType)
    {
        throw new UnexpectedMessageException(expectedType, actualType);
    }

}
