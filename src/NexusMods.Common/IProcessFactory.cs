using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusMods.Common;

/// <summary>
/// wrapper for <see cref="Process"/> so they can be mocked and tested
/// </summary>
public interface IProcess
{
}

/// <summary>
/// factory for OS processes, wraps the static part of the Process class so we can mock and test it
/// </summary>
public interface IProcessFactory
{
    /// <inheritdoc cref="Process.Start(ProcessStartInfo)"/>
    public IProcess? Start(ProcessStartInfo startInfo);
    /// <inheritdoc cref="Process.Start(string, string)"/>
    public IProcess? Start(string executable, string arguments);
}
