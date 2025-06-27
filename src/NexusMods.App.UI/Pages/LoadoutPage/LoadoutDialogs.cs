using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public static class LoadoutDialogs
{
    public static IDialog CreateCollection()
    {
        return DialogFactory.CreateStandardDialog(
            "Name your Collection",
            new StandardDialogParameters()
            {
                Text = "This is the name that will appear in the left hand menu and on the Collections page.",
                InputLabel = "Collection name",
                InputWatermark = "e.g. My Armour Mods",
            },
            [
                DialogStandardButtons.Cancel,
                new DialogButtonDefinition(
                    "Create",
                    ButtonDefinitionId.From("create"),
                    ButtonAction.Accept,
                    ButtonStyling.Primary
                ),
            ]
        );
    }

    public static IDialog RenameCollection(string collectionName)
    {
        return DialogFactory.CreateStandardDialog(
            $"Rename your collection \"{collectionName}\"",
            new StandardDialogParameters()
            {
                Text = $"Rename your existing collection \"{collectionName}\" to something else.",
                InputLabel = "New collection name",
                InputWatermark = "e.g. My Armour Mods",
                InputText = collectionName,
            },
            [
                DialogStandardButtons.Cancel,
                new DialogButtonDefinition(
                    "Rename",
                    ButtonDefinitionId.From("rename"),
                    ButtonAction.Accept,
                    ButtonStyling.Primary
                ),
            ]
        );
    }

    public static IDialog ShareCollection(string collectionName)
    {
        return DialogFactory.CreateStandardDialog(
            "Share Your Collection on Nexus Mods",
            new StandardDialogParameters()
            {
                Text = $"""
                      Upload "{collectionName}" to Nexus Mods to share it with friends or, if you choose, with the entire Nexus Mods community.
                      
                      Your collection will be added as a private draft until you publish it.
                      """,
                Icon = IconValues.PictogramUpload,
            },
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
            ]
        );
    }

    public static IDialog ShareCollectionSuccess(string collectionName)
    {
        return DialogFactory.CreateStandardDialog(
            "Your Collection Has Been Added as a Draft",
            new StandardDialogParameters()
            {
                Text = $"""
                    Click View Page to edit details and optionally publish your collection as either:
                    
                    • Listed – Anyone can discover this collection on Nexus Mods.
                    • Unlisted – Only people with the link can view it.
                    
                    You can change the visibility at any time in your collection settings on the Nexus Mods page.
                    """,
                Icon = IconValues.PictogramCelebrate,
            },
            [
                new DialogButtonDefinition("Close", ButtonDefinitionId.From("close"), ButtonAction.Reject),
                new DialogButtonDefinition("View page", ButtonDefinitionId.From("view-page"), ButtonAction.Accept,
                    ButtonStyling.Default, IconValues.OpenInNew
                ),
            ]
        );
    }

    public static IDialog UpdateCollection(string collectionName)
    {
        return DialogFactory.CreateStandardDialog(
            "Share Your Collection on Nexus Mods",
            new StandardDialogParameters()
            {
                Text = $"""
                     Upload "{collectionName}" to Nexus Mods to share it with friends or, if you choose, with the entire Nexus Mods community.
                     
                     Your collection will be added as a private draft until you publish it.
                     """,
                Icon = IconValues.PictogramUpload,
            },
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
            ]
        );
    }

    public static IDialog UpdateCollectionSuccess(string collectionName)
    {
        return DialogFactory.CreateStandardDialog(
            "Your Collection Has Been Updated",
            new StandardDialogParameters()
            {
                Text = $"""
                    You have successfully uploaded a new revision of "{collectionName}".
                    """,
                Icon = IconValues.PictogramCelebrate,
            },
            [
                new DialogButtonDefinition("Close", ButtonDefinitionId.From("close"), ButtonAction.Reject),
                new DialogButtonDefinition("View page", ButtonDefinitionId.From("view-page"), ButtonAction.Accept,
                    ButtonStyling.Default, IconValues.OpenInNew
                ),
            ]
        );
    }
}
