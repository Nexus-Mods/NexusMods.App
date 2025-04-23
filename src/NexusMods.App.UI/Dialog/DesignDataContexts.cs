using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Pages.Settings;

namespace NexusMods.App.UI.Dialog;

/// <summary>
/// ViewModel instances used in design mode
/// </summary>
internal static class DesignDataContexts
{
    public static MessageBoxViewModel MessageBoxDesignViewModel { get; } = new (
        "Delete this mod?", 
        "Deleting this mod will remove it from all collections. This action cannot be undone.",
        [MessageBoxStandardButtons.Ok, MessageBoxStandardButtons.Cancel]
        );
    
    public static CustomContentViewModel CustomContentDesignViewModel { get; } = new("Custom Text");

    public static DialogContainerViewModel DialogContainerDesignViewModel { get; } = new (
        CustomContentDesignViewModel,
        "Custom Window Title",
        650,
        true
    );

}
