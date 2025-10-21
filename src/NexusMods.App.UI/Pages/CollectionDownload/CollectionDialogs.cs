using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Pages.CollectionDownload.Dialogs.PremiumDownloads;
using NexusMods.App.UI.Resources;
using NexusMods.UI.Sdk.Dialog;
using NexusMods.UI.Sdk.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public static class CollectionDialogs
{
    public static IDialog PremiumCollectionDialog()
    {
        var premiumCollectionDownloadsViewModel = new DialogPremiumCollectionDownloadsViewModel();

        return DialogFactory.CreateDialog(Language.DialogPremiumCollection_DialogTitle,
            [
                new DialogButtonDefinition(
                    Language.DialogPremiumCollection_Cancel,
                    ButtonDefinitionId.From("cancel"),
                    ButtonAction.Reject
                ),
                new DialogButtonDefinition(
                    Language.DialogPremiumCollection_UpgradeToPremium,
                    ButtonDefinitionId.From("go-premium"),
                    ButtonAction.Accept,
                    ButtonStyling.Premium,
                    IconValues.Premium
                )
            ],
            premiumCollectionDownloadsViewModel
        );
    }

}
