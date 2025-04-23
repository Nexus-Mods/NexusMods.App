using Avalonia.Controls;
using NexusMods.App.UI.Dialog.Enums;

namespace NexusMods.App.UI.Dialog;

public interface IDialog<TReturn>
{
    public Task<TReturn> ShowWindow(Window? owner = null, bool isModal = false);
}
