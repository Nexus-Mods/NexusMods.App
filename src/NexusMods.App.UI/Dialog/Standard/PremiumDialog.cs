using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Windows;
using NexusMods.CrossPlatform.Process;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Dialog.Standard;

/// <summary>
/// Helper class for showing premium upgrade dialogs
/// </summary>
public static class PremiumDialog
{
    /// <summary>
    /// Shows the premium upgrade dialog for mod updates
    /// </summary>
    /// <param name="windowManager">Window manager to show the dialog</param>
    /// <param name="osInterop">OS interop service for opening URLs</param>
    /// <returns>Task representing the dialog result</returns>
    public static async Task<ButtonDefinitionId> ShowUpdatePremiumDialog(IWindowManager windowManager, IOSInterop osInterop)
    {
        var dialog = DialogFactory.CreateMessageDialog(
            title: "Go Premium for one-click mod updates",
            text: """
                No browser, no manual downloads. Premium users also get:
                
                • Download entire collections with one click
                • Uncapped download speeds
                • No Ads for life, even if you unsubscribe after 1 month!
                """,
            heading: "Update all your mods, or individual mods, in one click.",
            buttonDefinitions:
            [
                new DialogButtonDefinition(
                    "Update mods manually",
                    ButtonDefinitionId.From("update-manually"),
                    ButtonAction.Reject
                ),
                new DialogButtonDefinition(
                    "Go Premium",
                    ButtonDefinitionId.From("go-premium"),
                    ButtonAction.Accept,
                    ButtonStyling.Primary
                )
            ],
            icon: IconValues.PictogramPremium,
            dialogWindowSize: DialogWindowSize.Medium
        );

        var result = await windowManager.ShowDialog(dialog, DialogWindowType.Modal);
        
        if (result == ButtonDefinitionId.From("go-premium"))
        {
            var premiumUrl = new Uri("https://next.nexusmods.com/premium");
            await osInterop.OpenUrl(premiumUrl);
        }

        return result;
    }
} 
