using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls.Trees.Common;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

[ExcludeFromCodeCoverage]
public partial class PreviewTreeEntryView : ReactiveUserControl<IPreviewTreeEntryViewModel>
{
    public PreviewTreeEntryView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            // Initialize the view if the view model is not null
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Subscribe(_ => { InitView(); })
                .DisposeWith(disposable);

            this.BindCommand(ViewModel, vm => vm.RemoveMappingCommand,
                    v => v.XRoundedButton)
                .DisposeWith(disposable);

            this.OneWayBind(ViewModel, vm => vm.IsRemovable,
                    view => view.XRoundedButton.IsVisible)
                .DisposeWith(disposable);

            // FolderMerged pill, only show if it is a folder and it is merged.
            this.WhenAnyValue(view => view.ViewModel!.IsFolderMerged)
                .Subscribe(isMerged => FolderMergedPill.IsVisible = isMerged && ViewModel!.IsDirectory)
                .DisposeWith(disposable);
        });
    }

    private void InitView()
    {
        FileNameTextBlock.Text = ViewModel!.DisplayName;
        NewPill.IsVisible = ViewModel.IsNew;
        DupeFolderPill.IsVisible = ViewModel.IsFolderDupe;
        FolderMergedPill.IsVisible = ViewModel.IsFolderMerged && ViewModel.IsDirectory;

        // Always show unlink button, it means unlink child nodes if it is a folder.
        if (ViewModel.IsDirectory)
        {
            EntryIcon.Classes.Add("FolderOutline");

        }
        else
        {
            EntryIcon.Classes.Add(ViewModel.GamePath.Extension.GetIconClass());
        }
    }
}
