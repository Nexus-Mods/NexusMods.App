using System.Diagnostics;

namespace NexusMods.Common;

/// <summary>
/// Wrapper for <see cref="Process"/> so they can be mocked and tested
/// </summary>
public interface IProcess
{
    
}

/// <summary>
/// Factory for OS processes, wraps the static part of the Process class so we can mock and test it
/// </summary>
public interface IProcessFactory
{
    /// <inheritdoc cref="Process.Start(ProcessStartInfo)"/>
    public IProcess? Start(ProcessStartInfo startInfo);
    /// <inheritdoc cref="Process.Start(string, string)"/>
    public IProcess? Start(string executable, string arguments);
}
