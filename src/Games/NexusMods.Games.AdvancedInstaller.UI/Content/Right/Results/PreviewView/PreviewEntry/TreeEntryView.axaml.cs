using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;

public partial class TreeEntryView : ReactiveUserControl<ITreeEntryViewModel>
{
    public TreeEntryView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            if (ViewModel == null)
                return;

            InitView();
        });
    }

    private void InitView()
    {
        FileNameTextBlock.Text = ViewModel!.FileName;
        NewPill.IsVisible = ViewModel.IsNew;
        DupeFolderPill.IsVisible = ViewModel.IsFolderDuplicated;
        FolderMergedPill.IsVisible = ViewModel.IsFolderMerged;

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
