using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.App.UI.Resources;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.CollectionDownload.Dialogs.PremiumDownloads;

public partial class DialogPremiumCollectionDownloadsView : ReactiveUserControl<IDialogPremiumCollectionDownloadsViewModel>
{
    public DialogPremiumCollectionDownloadsView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
            {
                
            }
        );
    }
}
