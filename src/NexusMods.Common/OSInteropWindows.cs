using System.Diagnostics;

namespace NexusMods.Common;

/// <summary>
/// OS interoperation for windows
/// </summary>
// ReSharper disable once InconsistentNaming
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
    public void OpenUrl(string url)
    {
        _processFactory.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
