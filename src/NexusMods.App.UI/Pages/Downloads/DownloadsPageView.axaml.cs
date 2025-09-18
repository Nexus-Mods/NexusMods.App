using Avalonia.ReactiveUI;
using JetBrains.Annotations;

namespace NexusMods.App.UI.Pages.Downloads;

[UsedImplicitly]
public partial class DownloadsPageView : ReactiveUserControl<IDownloadsPageViewModel>
{
    public DownloadsPageView()
    {
        InitializeComponent();

        // TODO: Add WhenActivated block for bindings and reactive subscriptions
    }
}