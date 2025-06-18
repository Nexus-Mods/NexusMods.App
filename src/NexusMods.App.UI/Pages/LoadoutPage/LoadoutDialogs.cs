using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public static class LoadoutDialogs
{
    public static Dialog<DialogView, DialogViewModel, ButtonDefinitionId> ShareCollection(string collectionName)
    {
        return DialogFactory.CreateMessageBox("Share Your Collection on Nexus Mods",
            $"""
                    Upload "{collectionName}" to Nexus Mods to share it with friends or, if you choose, with the entire Nexus Mods community.
                    
                    Your collection will be added as a private draft until you publish it.
                    """,
            [
                new DialogButtonDefinition("Cancel", ButtonDefinitionId.From("cancel"), ButtonAction.Reject),
                new DialogButtonDefinition("Share to Nexus Mods", ButtonDefinitionId.From("share"), ButtonAction.Accept,
                    ButtonStyling.Primary
                ),
            ],
            IconValues.PictogramUpload,
            DialogWindowSize.Medium
        );
    }

    public static Dialog<DialogView, DialogViewModel, ButtonDefinitionId> ShareCollectionSuccess(string collectionName)
    {
        return DialogFactory.CreateMessageBox("Your Collection Has Been Added as a Draft",
            """
                    Click View Page to edit details and optionally publish your collection as either:
                    
                    • Listed – Anyone can discover this collection on Nexus Mods.
                    • Unlisted – Only people with the link can view it.
                    
                    You can change the visibility at any time in your collection settings on the Nexus Mods page.
                    """,
            [
                new DialogButtonDefinition("Close", ButtonDefinitionId.From("close"), ButtonAction.Reject),
                new DialogButtonDefinition("View page", ButtonDefinitionId.From("view-page"), ButtonAction.Accept,
                    ButtonStyling.Default, IconValues.OpenInNew
                ),
            ],
            IconValues.PictogramCelebrate,
            DialogWindowSize.Medium
        );
    }

    public static Dialog<DialogView, DialogViewModel, ButtonDefinitionId> UpdateCollection(string collectionName)
    {
        return DialogFactory.CreateMessageBox("Update Your Collection on Nexus Mods",
            $"""
                    Upload an update of "{collectionName}" to Nexus Mods.
                    
                    Your update will be uploaded as a new revision of the collection.
                    """,
            [
                new DialogButtonDefinition("Cancel", ButtonDefinitionId.From("cancel"), ButtonAction.Reject),
                new DialogButtonDefinition("Share to Nexus Mods", ButtonDefinitionId.From("share"), ButtonAction.Accept,
                    ButtonStyling.Primary
                ),
            ],
            IconValues.PictogramUpload,
            DialogWindowSize.Medium
        );
    }
    
    public static Dialog<DialogView, DialogViewModel, ButtonDefinitionId> UpdateCollectionSuccess(string collectionName)
    {
        return DialogFactory.CreateMessageBox("Your Collection Has Been Updated",
            $"""
                    You have successfully uploaded a new revision of "{collectionName}".
                    """,
            [
                new DialogButtonDefinition("Close", ButtonDefinitionId.From("close"), ButtonAction.Reject),
                new DialogButtonDefinition("View page", ButtonDefinitionId.From("view-page"), ButtonAction.Accept,
                    ButtonStyling.Default, IconValues.OpenInNew
                ),
            ],
            IconValues.PictogramCelebrate,
            DialogWindowSize.Medium
        );
    }
}
