using System.Collections.Concurrent;
using Avalonia.Controls;
using Bannerlord.LauncherManager.External.UI;
using Bannerlord.LauncherManager.Localization;
using Bannerlord.LauncherManager.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Providers;

internal sealed class NotificationProvider : INotificationProvider
{
    private readonly ConcurrentDictionary<string, object?> _notificationIds = new();
    private Window? _window; // TODO: How to inject the window?

    public void SendNotification(string id, NotificationType type, string message, uint displayMs)
    {
        if (_window is null)
        {
            return;
        }

        if (string.IsNullOrEmpty(id)) id = Guid.NewGuid().ToString();

        // Prevents message spam
        if (_notificationIds.TryAdd(id, null)) return;
        using var cts = new CancellationTokenSource();
        _ = Task.Delay(TimeSpan.FromMilliseconds(displayMs), cts.Token).ContinueWith(x => _notificationIds.TryRemove(id, out _), cts.Token);

        var translatedMessage = new BUTRTextObject(message).ToString();
        switch (type)
        {
            case NotificationType.Hint:
            {
                //HintManager.ShowHint(translatedMessage);
                cts.Cancel();
                break;
            }
            case NotificationType.Info:
            {
                // TODO:
                //HintManager.ShowHint(translatedMessage);
                cts.Cancel();
                break;
            }
            default:
                //MessageBox.Show(translatedMessage);
                cts.Cancel();
                break;
        }
    }
}
