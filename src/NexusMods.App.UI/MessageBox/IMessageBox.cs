using Avalonia.Controls;
using NexusMods.App.UI.MessageBox.Enums;

namespace NexusMods.App.UI.MessageBox;

public interface IMessageBox<T>
{
    public Task<T> ShowWindow(Window? owner = null, bool isDialog = false);
}
