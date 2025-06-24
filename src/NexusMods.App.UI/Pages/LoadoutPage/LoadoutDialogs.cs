using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public static class LoadoutDialogs
{
    public static Dialog<DialogView, MessageDialogViewModel, ButtonDefinitionId> ShareCollection(string collectionName)
    {
        return DialogFactory.CreateMessageDialog(
            title: "Share Your Collection on Nexus Mods",
            text: $"""
                    Upload "{collectionName}" to Nexus Mods to share it with friends or, if you choose, with the entire Nexus Mods community.
                    
                    Your collection will be added as a private draft until you publish it.
                    """,
            buttonDefinitions:
            [
                new DialogButtonDefinition(
                    "Cancel",
                    ButtonDefinitionId.From("cancel"),
                    ButtonAction.Reject
                ),
                new DialogButtonDefinition(
                    "Share to Nexus Mods",
                    ButtonDefinitionId.From("share"),
                    ButtonAction.Accept,
                    ButtonStyling.Primary
                ),
            ],
            icon: IconValues.PictogramUpload,
            dialogWindowSize: DialogWindowSize.Medium
        );
    }

    public static Dialog<DialogView, MessageDialogViewModel, ButtonDefinitionId> ShareCollectionSuccess(string collectionName)
    {
        return DialogFactory.CreateMessageDialog(
            title: "Your Collection Has Been Added as a Draft",
            text: """
                    Click View Page to edit details and optionally publish your collection as either:
                    
                    • Listed – Anyone can discover this collection on Nexus Mods.
                    • Unlisted – Only people with the link can view it.
                    
                    You can change the visibility at any time in your collection settings on the Nexus Mods page.
                    """,
            buttonDefinitions:
            [
                new DialogButtonDefinition("Close", ButtonDefinitionId.From("close"), ButtonAction.Reject),
                new DialogButtonDefinition("View page", ButtonDefinitionId.From("view-page"), ButtonAction.Accept,
                    ButtonStyling.Default, IconValues.OpenInNew
                ),
            ],
            icon: IconValues.PictogramCelebrate,
            dialogWindowSize: DialogWindowSize.Medium
        );
    }

    public static Dialog<DialogView, MessageDialogViewModel, ButtonDefinitionId> UpdateCollection(string collectionName)
    {
        return DialogFactory.CreateMessageDialog(
            title: "Update Your Collection on Nexus Mods",
            text: $"""
                    Upload an update of "{collectionName}" to Nexus Mods.
                    
                    Your update will be uploaded as a new revision of the collection.
                    """,
            buttonDefinitions:
            [
                new DialogButtonDefinition("Cancel", ButtonDefinitionId.From("cancel"), ButtonAction.Reject),
                new DialogButtonDefinition("Share to Nexus Mods", ButtonDefinitionId.From("share"), ButtonAction.Accept,
                    ButtonStyling.Primary
                ),
            ],
            icon: IconValues.PictogramUpload,
            dialogWindowSize: DialogWindowSize.Medium
        );
    }

    public static Dialog<DialogView, MessageDialogViewModel, ButtonDefinitionId> UpdateCollectionSuccess(string collectionName)
    {
        return DialogFactory.CreateMessageDialog(
            title: "Your Collection Has Been Updated",
            text: $"""
                    You have successfully uploaded a new revision of "{collectionName}".
                    """,
            buttonDefinitions:
            [
                new DialogButtonDefinition("Close", ButtonDefinitionId.From("close"), ButtonAction.Reject),
                new DialogButtonDefinition("View page", ButtonDefinitionId.From("view-page"), ButtonAction.Accept,
                    ButtonStyling.Default, IconValues.OpenInNew
                ),
            ],
            icon: IconValues.PictogramCelebrate,
            dialogWindowSize: DialogWindowSize.Medium
        );
    }
}
