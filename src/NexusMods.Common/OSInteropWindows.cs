using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusMods.Common;

/// <summary>
/// OS interoperation for windows
/// </summary>
public class OSInteropWindows : IOSInterop
{
    private readonly IProcessFactory _processFactory;
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="processFactory"></param>
    public OSInteropWindows(IProcessFactory processFactory)
    {
        _processFactory = processFactory;
    }

    /// <inheritdoc/>
    public void OpenURL(string url)
    {
        _processFactory.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
