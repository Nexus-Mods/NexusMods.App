using System.Diagnostics;

namespace NexusMods.Common;

/// <summary>
/// wrapper for <see cref="Process"/>es so they can be mocked and tested
/// </summary>
public class ProcessWrap : IProcess
{
    // Assuming Tannin will want to use this for tests later so suppressing for now, Sewer
    // ReSharper disable once NotAccessedField.Local
    private Process _proc;

    /// <summary/>
    public ProcessWrap(Process proc)
    {
        _proc = proc;
    }
}

/// <summary>
/// concrete implementation of <see cref="IProcessFactory"/> using actual os processes
/// </summary>
public class ProcessFactory : IProcessFactory
{
    /// <inheritdoc/>
    public IProcess? Start(ProcessStartInfo startInfo)
    {
        var res = Process.Start(startInfo);
        return (res != null) ? new ProcessWrap(res) : null;
    }

    /// <inheritdoc/>
    public IProcess Start(string executable, string arguments)
    {
        var res = Process.Start(executable, arguments);
        return new ProcessWrap(res);
    }
}
