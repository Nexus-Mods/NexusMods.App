using NexusMods.App.UI.Dialog.Enums;

namespace NexusMods.App.UI.Dialog;

/// <summary>
/// ViewModel instances used in design mode
/// </summary>
internal static class DesignDataContexts
{
    public static MessageBoxViewModel MessageBoxViewModel { get; } = new (
        "Delete this mod?", 
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Cancel]
        );

    public static DialogContainerViewModel DialogContainerViewModel { get; } = new();
}
