using System.Net.Mime;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.UI.Sdk;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutPage.Dialogs.CollectionPublished;

public partial class DialogCollectionPublishedView : ReactiveUserControl<IDialogCollectionPublishedViewModel>
{
    public DialogCollectionPublishedView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel!.CollectionStatus)
                .Subscribe(status =>
                {
                    TextDescription.Text = status switch
                    {
                        CollectionStatus.Listed => $"{ViewModel!.CollectionName} has been published as unlisted. Only people with the link can view it.",
                        CollectionStatus.Unlisted => $"{ViewModel!.CollectionName} has been published as listed. It will appear in search results and may be featured.",
                    };
                })
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.CollectionUrl, view => view.TextBoxUrl.Text, url => url.AbsoluteUri)
                .DisposeWith(disposables);
        });
    }
}

