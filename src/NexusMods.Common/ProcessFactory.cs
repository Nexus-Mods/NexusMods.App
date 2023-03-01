using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusMods.Common;

/// <summary>
/// wrapper for <see cref="Process"/>es so they can be mocked and tested
/// </summary>
public class ProcessWrap : IProcess
{
    private Process _proc;
    /// <summary>
    /// constructor
    /// </summary>
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
    public IProcess? Start(string executable, string arguments)
    {
        var res = Process.Start(executable, arguments);
        return (res != null) ? new ProcessWrap(res) : null;
    }
}
