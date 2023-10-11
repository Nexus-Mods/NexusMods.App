using Avalonia.ReactiveUI;
using NexusMods.App.UI;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public partial class
    TreeEntryView : ReactiveUserControl<ITreeEntryViewModel>
{
    public TreeEntryView()
    {
        InitializeComponent();

        this.WhenActivated(disposable =>
        {
            switch (ViewModel!.Node.Value)
            {
                case IModContentNode contentNode:
                    PupulateFromModContentNode(contentNode);

                    this.WhenAnyValue(x => x.ViewModel!.Node.AsT0.Status)
                        .SubscribeWithErrorLogging(status =>
                        {
                            UpdateFromStatus(ViewModel!.Node.AsT0);
                        });
                    break;

                case ISelectableDirectoryNode selectableDirectoryNode:
                    PopulateFromSelectableDirectoryNode(selectableDirectoryNode);
                    break;

                case IPreviewEntryNode previewNode:
                    PopulateFromPreviewNode(previewNode);
                    break;
            }

        });
    }

    private void PupulateFromModContentNode(IModContentNode node)
    {
        FileElementGrid.IsVisible = true;
        FileNameTextBlock.IsVisible = true;

        if (node.IsRoot)
        {
            FileNameTextBlock.Text = "All mod files";

            InstallRoundedButtonTextBlock.Text = "Install all";
        }
        else
        {
            FileNameTextBlock.Text = node.FileName;

            if (node.IsDirectory)
            {
                FolderEntryIcon.IsVisible = true;
                InstallRoundedButtonTextBlock.Text = "Install folder";
            }
            else
            {
                FileEntryIcon.IsVisible = true;
                InstallRoundedButtonTextBlock.Text = "Install";
            }
        }
    }

    private void UpdateFromStatus(IModContentNode node)
    {
        var status = node.Status;
        ClearAllButtons();
        switch (status)
        {
            case TreeDataGridSourceFileNodeStatus.Default:
                InstallRoundedButton.IsVisible = true;
                break;
            case TreeDataGridSourceFileNodeStatus.Selecting:
                SelectLocationRoundedButton.IsVisible = true;
                break;
            case TreeDataGridSourceFileNodeStatus.SelectingViaParent:
                if (node.IsDirectory)
                {
                    IncludeTransitionButtonTextBlock.Text = "Include folder";
                } else
                {
                    // TODO: This should be "Include" if the parent is the root node, otherwise "Include with folder"
                    IncludeTransitionButtonTextBlock.Text = "Include with folder";
                }
                IncludeTransitionButton.IsVisible = true;
                break;
            case TreeDataGridSourceFileNodeStatus.IncludedExplicit:
                RemoveFromLocationButtonTextBlock.Text = node.LinkedNode?.DirectoryName;
                RemoveFromLocationButton.IsVisible = true;
                break;
            case TreeDataGridSourceFileNodeStatus.IncludedViaParent:
                if (node.IsDirectory)
                {
                    IncludedRemoveButtonTextBlock.Text = "Included folder";
                } else
                {
                    // TODO: This should be "Included" if the parent is the root node, otherwise "Included with folder"
                    IncludedRemoveButtonTextBlock.Text = "Included with folder";
                }
                IncludedRemoveButton.IsVisible = true;
                break;

        }
    }

    private void PopulateFromPreviewNode(IPreviewEntryNode node)
    {

    }

    private void PopulateFromSelectableDirectoryNode(ISelectableDirectoryNode node)
    {

    }

    private void ClearAllButtons()
    {
        InstallRoundedButton.IsVisible = false;
        SelectLocationRoundedButton.IsVisible = false;
        XRoundedButton.IsVisible = false;
        RemoveFromLocationButton.IsVisible = false;
        IncludeTransitionButton.IsVisible = false;
        IncludedRemoveButton.IsVisible = false;
        SelectRoundedButton.IsVisible = false;
        DeleteCreatedFolderButton.IsVisible = false;
        CancelCreateFolderButton.IsVisible = false;
        SaveCreatedFolderButton.IsVisible = false;
    }
}
