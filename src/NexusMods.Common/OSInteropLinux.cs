using System.Diagnostics;

namespace NexusMods.Common;

/// <summary>
/// OS interoperation for linux
/// </summary>
public class OSInteropLinux : IOSInterop
{
    private readonly IProcessFactory _processFactory;
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="processFactory"></param>
    public OSInteropLinux(IProcessFactory processFactory)
    {
        _processFactory = processFactory;
    }

    /// <inheritdoc/>
    public void OpenURL(string url)
    {
        _processFactory.Start("xdg-open", url);
    }
}
