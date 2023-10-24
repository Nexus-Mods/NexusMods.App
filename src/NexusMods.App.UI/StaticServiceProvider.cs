using System.Diagnostics;
using Avalonia.Threading;

namespace NexusMods.App.UI;

public static class StaticServiceProvider
{
    private static IServiceProvider? _serviceProvider;

    public static void Set(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static IServiceProvider Get()
    {
        Debug.Assert(Dispatcher.UIThread.CheckAccess());
        return _serviceProvider ?? throw new InvalidOperationException();
    }
}
