using Avalonia.Controls;
using NexusMods.App.UI.Dialog.Enums;

namespace NexusMods.App.UI.Dialog;

public interface IDialog
{
    public Task<StandardDialogResult> Show(Window owner, bool isModal = true);
}
