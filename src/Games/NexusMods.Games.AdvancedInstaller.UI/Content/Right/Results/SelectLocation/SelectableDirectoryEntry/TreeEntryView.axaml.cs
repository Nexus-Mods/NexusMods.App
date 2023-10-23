using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

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
        switch (ViewModel!.Status)
        {
            case SelectableDirectoryNodeStatus.Regular:
                FileElementGrid.IsVisible = true;
                FolderEntryIcon.IsVisible = true;
                FileNameTextBlock.IsVisible = true;

                FileNameTextBlock.Text = ViewModel.DisplayName;

                SelectRoundedButton.IsVisible = true;
                break;

            case SelectableDirectoryNodeStatus.Create:
                FileElementGrid.IsVisible = false;
                CreateFolderButton.IsVisible = true;
                break;

            case SelectableDirectoryNodeStatus.Editing:
                FileElementGrid.IsVisible = true;
                FolderEntryIcon.IsVisible = true;

                // input field
                CreateFolderNameTextBox.IsVisible = true;

                // buttons
                CancelCreateFolderButton.IsVisible = true;
                SaveCreatedFolderButton.IsVisible = true;
                break;

            case SelectableDirectoryNodeStatus.Created:
                FileElementGrid.IsVisible = true;
                FolderEntryIcon.IsVisible = true;
                FileNameTextBlock.IsVisible = true;
                FileNameTextBlock.Text = ViewModel.DisplayName;
                SelectRoundedButton.IsVisible = true;
                DeleteCreatedFolderButton.IsVisible = true;
                break;
        }
    }
}
