using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Notifications;

public class CustomNotificationViewModel
{
    public string Message { get; set; }
    
    public CustomNotificationViewModel(string message)
    {
        Message = message;
    }
}
