using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public static class LoadoutDialogs
{
    public static Dialog<DialogView, InputDialogViewModel, InputDialogResult> CreateCollection()
    {
        return DialogFactory.CreateInputDialog(
            title: "Name your Collection",
            buttonDefinitions: [
                DialogStandardButtons.Cancel,
                new DialogButtonDefinition(
                    "Create", 
                    ButtonDefinitionId.From("create"), 
                    ButtonAction.Accept,
                    ButtonStyling.Primary
                ),
            ],
            text: "This is the name that will appear in the left hand menu and on the Collections page.",
            inputLabel: "Collection name",
            inputWatermark: "e.g. My Armour Mods",
            dialogWindowSize: DialogWindowSize.Medium
        );
    }

    public static Dialog<DialogView, InputDialogViewModel, InputDialogResult> RenameCollection(string collectionName)
    {
        return DialogFactory.CreateInputDialog(
            title: $"Rename your collection \"{collectionName}\"",
            buttonDefinitions: [
                DialogStandardButtons.Cancel,
                new DialogButtonDefinition(
                    "Rename", 
                    ButtonDefinitionId.From("rename"), 
                    ButtonAction.Accept,
                    ButtonStyling.Primary
                ),
            ],
            text: $"Rename your existing collection \"{collectionName}\" to something else.",
            inputLabel: "New collection name",
            inputWatermark: "e.g. My Armour Mods",
            dialogWindowSize: DialogWindowSize.Medium
        );
    }
    
    public static Dialog<DialogView, MessageDialogViewModel, ButtonDefinitionId> ShareCollection(string collectionName)
    {
         return DialogFactory.CreateDialog(
             title: "Share Your Collection on Nexus Mods",
//              text: $"""
//                      Upload "{collectionName}" to Nexus Mods to share it with friends or, if you choose, with the entire Nexus Mods community.
//                      
//                      Your collection will be added as a private draft until you publish it.
//                      """,
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
             contentViewModel: null!
             // icon: IconValues.PictogramUpload,
             // dialogWindowSize: DialogWindowSize.Medium
         );
    }

    public static IDialog ShareCollectionSuccess(string collectionName)
    {
        return DialogFactory.CreateDialog(
            title: "Share Your Collection on Nexus Mods",
//              text: $"""
//                      Upload "{collectionName}" to Nexus Mods to share it with friends or, if you choose, with the entire Nexus Mods community.
//                      
//                      Your collection will be added as a private draft until you publish it.
//                      """,
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
            contentViewModel: null!
            // icon: IconValues.PictogramUpload,
            // dialogWindowSize: DialogWindowSize.Medium
        );
    }

    public static IDialog UpdateCollection(string collectionName)
    {
        return DialogFactory.CreateDialog(
            title: "Share Your Collection on Nexus Mods",
//              text: $"""
//                      Upload "{collectionName}" to Nexus Mods to share it with friends or, if you choose, with the entire Nexus Mods community.
//                      
//                      Your collection will be added as a private draft until you publish it.
//                      """,
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
            contentViewModel: null!
            // icon: IconValues.PictogramUpload,
            // dialogWindowSize: DialogWindowSize.Medium
        );
    }

    public static IDialog UpdateCollectionSuccess(string collectionName)
    {
        return DialogFactory.CreateDialog(
            title: "Share Your Collection on Nexus Mods",
//              text: $"""
//                      Upload "{collectionName}" to Nexus Mods to share it with friends or, if you choose, with the entire Nexus Mods community.
//                      
//                      Your collection will be added as a private draft until you publish it.
//                      """,
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
            contentViewModel: null!
            // icon: IconValues.PictogramUpload,
            // dialogWindowSize: DialogWindowSize.Medium
        );
    }
}
