using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
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
            if (ViewModel == null)
                return;

            this.BindCommand(ViewModel, vm => vm.RemoveMappingCommand, v => v.XRoundedButton)
                .DisposeWith(disposable);

            this.WhenAnyValue(view => view.ViewModel!.IsFolderMerged)
                .Subscribe(isMerged => FolderMergedPill.IsVisible = isMerged && ViewModel.IsDirectory)
                .DisposeWith(disposable);

            this.OneWayBind(ViewModel,vm => vm.IsRemovable, view => view.XRoundedButton.IsVisible)
                .DisposeWith(disposable);

            InitView();
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
            FolderEntryIcon.IsVisible = true;

            // Make directory name bold.
            FileNameTextBlock.Classes.Remove("BodyMDNormal");
            FileNameTextBlock.Classes.Add("BodyMDBold");
        }
        else
        {
            FileEntryIcon.IsVisible = true;
        }
    }
}
