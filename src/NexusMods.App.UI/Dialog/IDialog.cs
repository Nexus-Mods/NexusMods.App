using Avalonia.Controls;
using NexusMods.App.UI.Dialog.Enums;

namespace NexusMods.App.UI.Dialog;

public interface IDialog<TResult>
{
    public Task<TResult?> ShowWindow(Window owner, bool isModal = false);
}
