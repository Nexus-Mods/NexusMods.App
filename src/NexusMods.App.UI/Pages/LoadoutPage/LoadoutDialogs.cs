using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public static class LoadoutDialogs
{
    public static IDialog ShareCollection(string collectionName)
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
