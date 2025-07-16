using System.Net.Mime;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.App.UI.Resources;
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
                this.WhenAnyValue(view => view.ViewModel!.CollectionStatus,
                        view => view.ViewModel!.FirstPublish
                    )
                    .Subscribe(tuple =>
                        {
                            var (status, firstPublish) = tuple;

                            if (firstPublish)
                            {
                                TextDescription.Text = status == CollectionStatus.Listed
                                
                                    ? string.Format(Language.Loadout_Dialog_CollectionPublished_FirstPublish_Listed, ViewModel!.CollectionName)
                                    : string.Format(Language.Loadout_Dialog_CollectionPublished_FirstPublish_Unlisted, ViewModel!.CollectionName);

                                TextHelp.Text = string.Format(Language.Loadout_Dialog_CollectionPublished_FirstPublish_Help);
                            }
                            else
                            {
                                TextDescription.Text = string.Format(Language.Loadout_Dialog_CollectionPublished_Revision, ViewModel!.CollectionName);
                                TextHelp.Text = string.Format(Language.Loadout_Dialog_CollectionPublished_Revision_Help);
                            }
                        }
                    )
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                        vm => vm.CollectionUrl,
                        view => view.TextBoxUrl.Text,
                        uri => uri.AbsoluteUri
                    )
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                        vm => vm.CommandCopyUrl,
                        v => v.ButtonCopyToClipboard
                    )
                    .DisposeWith(disposables);
            }
        );
    }
}
