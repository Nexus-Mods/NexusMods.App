using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Abstractions.UI;
using R3;

namespace NexusMods.App.UI.Pages.CollectionDownload.Dialogs.PremiumDownloads;

public interface IDialogPremiumCollectionDownloadsViewModel : IViewModelInterface
{
}

public class DialogPremiumCollectionDownloadsViewModel : AViewModel<IDialogPremiumCollectionDownloadsViewModel>, IDialogPremiumCollectionDownloadsViewModel
{

    public DialogPremiumCollectionDownloadsViewModel()
    {
    }
}
