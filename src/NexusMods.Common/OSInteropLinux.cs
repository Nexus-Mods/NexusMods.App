namespace NexusMods.Common;

/// <summary>
/// OS interoperation for linux
/// </summary>
// ReSharper disable once InconsistentNaming
public class OSInteropLinux : IOSInterop
{
    private readonly IProcessFactory _processFactory;

    /// <summary/>
    public OSInteropLinux(IProcessFactory processFactory)
    {
        _processFactory = processFactory;
    }

    /// <inheritdoc/>
    public void OpenUrl(string url)
    {
        _processFactory.Start("xdg-open", url);
    }
}
