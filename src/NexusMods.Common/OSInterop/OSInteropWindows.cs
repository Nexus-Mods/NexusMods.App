using System.Diagnostics;

namespace NexusMods.Common.OSInterop;

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
    public async Task OpenURL(string url, CancellationToken cancellationToken = default)
    {
        var process = new ProcessStartInfo(url) { UseShellExecute = true };
        var started = Process.Start(process);
        if (started != null)
            await started.WaitForExitAsync(cancellationToken);
    }
}
