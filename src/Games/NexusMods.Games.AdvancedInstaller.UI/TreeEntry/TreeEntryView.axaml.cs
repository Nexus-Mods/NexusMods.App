using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.App.UI;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Games.AdvancedInstaller.UI.Resources;
using NexusMods.Games.AdvancedInstaller.UI.Resources;
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
            if (ViewModel == null)
            {
                return;
            }
            switch (ViewModel!.Node.Value)
            {
                case IModContentNode contentNode:
                    PupulateFromModContentNode(contentNode);

                    this.WhenAnyValue(x => x.ViewModel!.Node.AsT0.Status)
                        .SubscribeWithErrorLogging(status => { UpdateFromStatus(ViewModel!.Node.AsT0); })
                        .DisposeWith(disposable);
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
            FileNameTextBlock.Text = Language.TreeEntryView_FileNameTextBlock_All_mod_files;

            InstallRoundedButtonTextBlock.Text = Language.TreeEntryView_InstallRoundedButtonTextBlock_Install_all;
        }
        else
        {
            FileNameTextBlock.Text = node.FileName;

            if (node.IsDirectory)
            {
                FolderEntryIcon.IsVisible = true;
                InstallRoundedButtonTextBlock.Text =
                    Language.TreeEntryView_InstallRoundedButtonTextBlock_Install_folder;
            }
            else
            {
                FileEntryIcon.IsVisible = true;
                InstallRoundedButtonTextBlock.Text = Language.TreeEntryView_InstallRoundedButtonTextBlock_Install;
            }
        }
    }

    private void UpdateFromStatus(IModContentNode node)
    {
        var status = node.Status;
        ClearAllButtons();
        switch (status)
        {
            case ModContentNodeStatus.Default:
                InstallRoundedButton.IsVisible = true;
                break;

            case ModContentNodeStatus.Selecting:
                SelectLocationRoundedButton.IsVisible = true;
                break;

            case ModContentNodeStatus.SelectingViaParent:
                if (node.IsDirectory)
                {
                    IncludeTransitionButtonTextBlock.Text =
                        Language.TreeEntryView_IncludeTransitionButtonTextBlock_Include_folder;
                }
                else
                {
                    IncludeTransitionButtonTextBlock.Text = node.IsTopLevel
                        ? Language.TreeEntryView_IncludeTransitionButtonTextBlock_Include
                        : Language.TreeEntryView_IncludeTransitionButtonTextBlock_Include_with_folder;
                }

                IncludeTransitionButton.IsVisible = true;
                break;

            case ModContentNodeStatus.IncludedExplicit:
                RemoveFromLocationButtonTextBlock.Text = node.LinkedTarget?.DirectoryName;
                RemoveFromLocationButton.IsVisible = true;
                break;

            case ModContentNodeStatus.IncludedViaParent:
                if (node.IsDirectory)
                {
                    IncludedRemoveButtonTextBlock.Text =
                        Language.TreeEntryView_IncludedRemoveButtonTextBlock_Included_folder;
                }
                else
                {
                    IncludedRemoveButtonTextBlock.Text = node.IsTopLevel
                        ? Language.TreeEntryView_IncludedRemoveButtonTextBlock_Included
                        : Language.TreeEntryView_IncludedRemoveButtonTextBlock_Included_with_folder;
                }

                IncludedRemoveButton.IsVisible = true;
                break;
        }
    }

    private void PopulateFromPreviewNode(IPreviewEntryNode node) { }

    private void PopulateFromSelectableDirectoryNode(ISelectableDirectoryNode node) { }

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
