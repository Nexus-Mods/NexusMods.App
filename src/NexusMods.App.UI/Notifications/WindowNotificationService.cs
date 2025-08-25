using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using NexusMods.App.UI.Dialog;
using NexusMods.UI.Sdk;

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
    
    public bool Show(
        string message, 
        ToastNotificationVariant type = ToastNotificationVariant.Neutral,
        TimeSpan? expiration = null, 
        Action? onClick = null, 
        Action? onClose = null)
    {
        var manager = GetNotificationManager();
        if (manager == null) return false;
        
        // TODO: Use ToastNotificationVariant
        
        var notification = new Notification(
            null,
            message,
            NotificationType.Information,
            expiration ?? TimeSpan.FromSeconds(5),
            onClick,
            onClose);
        
        manager.Show(notification);
        
        return true;
    }

    public bool Show(
        string message,
        ToastNotificationVariant type = ToastNotificationVariant.Neutral,
        TimeSpan? expiration = null,
        DialogButtonDefinition[]? buttonDefinitions = null,
        Action<ButtonDefinitionId>? buttonHandler = null)
    {
        var manager = GetNotificationManager();
        if (manager == null) return false;
        
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


