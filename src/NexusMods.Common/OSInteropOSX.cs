using System.Diagnostics;

namespace NexusMods.Common;

/// <summary>
/// OS interoperation for MacOS
/// </summary>
public class OSInteropOSX : IOSInterop
{
    private readonly IProcessFactory _processFactory;
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="processFactory"></param>
    public OSInteropOSX(IProcessFactory processFactory)
    {
        _processFactory = processFactory;
    }

    /// <inheritdoc/>
    public void OpenURL(string url)
    {
        _processFactory.Start("open", url);
    }
}
