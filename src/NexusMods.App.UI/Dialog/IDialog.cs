using Avalonia.Controls;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Dialog.Enums;

namespace NexusMods.App.UI.Dialog;

public interface IDialog
{
    public Task<ButtonDefinitionId> ShowWindow(Window owner, bool isModal = false);
}
