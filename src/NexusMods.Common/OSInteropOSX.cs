namespace NexusMods.Common;

/// <summary>
/// OS interoperation for MacOS
/// </summary>
// ReSharper disable once InconsistentNaming
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
    public void OpenUrl(string url)
    {
        _processFactory.Start("open", url);
    }
}
