using System.Diagnostics;
using Avalonia.Threading;
using JetBrains.Annotations;

namespace NexusMods.App.UI;

[PublicAPI]
public static class DesignerUtils
{
    private static bool _isDesigner;
    private static IServiceProvider? _serviceProvider;

    public static void Activate(IServiceProvider serviceProvider)
    {
        _isDesigner = true;
        _serviceProvider = serviceProvider;
    }

    public static bool IsDesigner()
    {
        Dispatcher.UIThread.VerifyAccess();
        return _isDesigner;
    }

    public static IServiceProvider GetServiceProvider()
    {
        Dispatcher.UIThread.VerifyAccess();
        AssertInDesigner();
        return _serviceProvider ?? throw new UnreachableException();
    }

    public static void AssertInDesigner()
    {
        if (_isDesigner) return;
        throw new InvalidOperationException("Not in designer!");
    }
}
