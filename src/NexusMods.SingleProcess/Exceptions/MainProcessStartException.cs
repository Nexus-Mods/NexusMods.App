using System;

namespace NexusMods.SingleProcess.Exceptions;


/// <summary>
/// Thrown when the main process fails to start.
/// </summary>
public class MainProcessStartException : Exception
{
    /// <summary>
    /// Primary constructor
    /// </summary>
    /// <param name="path"></param>
    /// <param name="argument"></param>
    public MainProcessStartException(string path, string argument) : base($"Failed to start main process: {path} with argument: {argument}")
    {
    }

}
