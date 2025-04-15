using NexusMods.App.UI.MessageBox.Enums;

namespace NexusMods.App.UI.MessageBox;

/// <summary>
/// ViewModel instances used in design mode
/// </summary>
internal static class DesignDataContexts
{
    public static MessageBoxStandardViewModel MessageBoxStandardViewModel { get; }

    static DesignDataContexts()
    {
        MessageBoxStandardViewModel = new MessageBoxStandardViewModel("This is a title", "This is my message text", 
            ButtonEnum.OkCancel);
    }
    
}
