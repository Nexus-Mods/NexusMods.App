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
        
        // Must be on UI thread to create the WindowNotificationManager
        DispatcherHelper.EnsureOnUIThread(() =>
            {
                _notificationManager = new WindowNotificationManager(desktopLifetime.MainWindow)
                {
                    Position = NotificationPosition.BottomCenter,
                    MaxItems = 4,
                };
            }
        );

        return _notificationManager;
    }

    /// <Inheritdoc />
    public void ShowToast(
        string message,
        ToastNotificationVariant type = ToastNotificationVariant.Neutral,
        TimeSpan? expiration = null,
        DialogButtonDefinition[]? buttonDefinitions = null,
        Action<ButtonDefinitionId>? buttonHandler = null)
    {
        DispatcherHelper.EnsureOnUIThread(() =>
            {
                var manager = GetNotificationManager();
                if (manager == null) return;
                
                // TODO: Use ToastNotificationVariant
                // TODO: Use buttons and handler
        
                var notification = new Notification(
                    null,
                    message,
                    NotificationType.Information,
                    expiration ?? TimeSpan.FromSeconds(5));
                
                // Must be on UI thread to show the notification
                manager.Show(notification);
                return;
            }
        );
    }
}


