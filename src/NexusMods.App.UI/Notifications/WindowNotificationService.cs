using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Dialog;

namespace NexusMods.App.UI.Notifications;

public class WindowNotificationService : IWindowNotificationService
{
    private WindowNotificationManager? _notificationManager;

    /// <summary>
    /// Lazy initialization, as main window may not available at creation time
    /// </summary>
    /// <returns></returns>
    private WindowNotificationManager? GetNotificationManager()
    {
        if (_notificationManager != null)
            return _notificationManager;

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktopLifetime)
        {
            // Unable to access the main window, so we cannot show notifications
            return null;
        }
        
        _notificationManager = new WindowNotificationManager(desktopLifetime.MainWindow)
        {
            Position = NotificationPosition.BottomCenter,
            MaxItems = 4,
        };

        return _notificationManager;
    }

    /// <Inheritdoc />
    public bool ShowToast(
        string message,
        ToastNotificationVariant type = ToastNotificationVariant.Neutral,
        TimeSpan? expiration = null,
        DialogButtonDefinition[]? buttonDefinitions = null,
        Action<ButtonDefinitionId>? buttonHandler = null)
    {
        var manager = GetNotificationManager();
        if (manager == null) return false;
        
        // Check if we are on the UI thread
        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            return ShowToastInternal(manager, message, type, expiration, buttonDefinitions, buttonHandler);
        
        // If not, marshal the call to the UI thread
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ShowToastInternal(manager, message, type, expiration, buttonDefinitions, buttonHandler);
        });
        return true;
    }

    private static bool ShowToastInternal(
        WindowNotificationManager manager,
        string message,
        ToastNotificationVariant type = ToastNotificationVariant.Neutral,
        TimeSpan? expiration = null,
        DialogButtonDefinition[]? buttonDefinitions = null,
        Action<ButtonDefinitionId>? buttonHandler = null)
    {
        // TODO: Use ToastNotificationVariant
        // TODO: Use buttons and handler
        
        var notification = new Notification(
            null,
            message,
            NotificationType.Information,
            expiration ?? TimeSpan.FromSeconds(5));
        
        manager.Show(notification);
        
        return true;
    }
}


