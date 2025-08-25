using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.CrossPlatform.Process;
using NexusMods.UI.Sdk.Dialog;
using NexusMods.UI.Sdk.Dialog.Enums;
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
        var dialog = DialogFactory.CreateStandardDialog(
            title: Language.PremiumDialog_UpdateTitle,
            new StandardDialogParameters()
            {
                Heading = Language.PremiumDialog_UpdateHeading,
                Text = Language.PremiumDialog_UpdateDescription,
                Icon = IconValues.PictogramPremium,
            },
            buttonDefinitions:
            [
                new DialogButtonDefinition(
                    Language.PremiumDialog_UpdateManuallyButton,
                    ButtonDefinitionId.From("update-manually"),
                    ButtonAction.Reject
                ),
                new DialogButtonDefinition(
                    Language.PremiumDialog_GoPremiumButton,
                    ButtonDefinitionId.From("go-premium"),
                    ButtonAction.Accept,
                    ButtonStyling.Primary
                )
            ],
            windowSize: DialogWindowSize.Medium
        );

        var result = await windowManager.ShowDialog(dialog, DialogWindowType.Modal);
        
        if (result.ButtonId == ButtonDefinitionId.From("go-premium"))
        {
            var premiumUrl = new Uri("https://next.nexusmods.com/premium");
            await osInterop.OpenUrl(premiumUrl);
        }

        return result.ButtonId;
    }
} 
