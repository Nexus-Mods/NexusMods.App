using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.Games.AdvancedInstaller.UI.Resources;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

[ExcludeFromCodeCoverage]
public partial class
    TreeEntryView : ReactiveUserControl<ITreeEntryViewModel>
{
    public TreeEntryView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            if (ViewModel == null)
                return;

            // Command bindings:
            this.BindCommand(ViewModel, vm => vm.BeginSelectCommand, view => view.InstallRoundedButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.CancelSelectCommand, view => view.SelectLocationRoundedButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.CancelSelectCommand, view => view.IncludeTransitionButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.UnlinkCommand, view => view.RemoveFromLocationButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.UnlinkCommand, view => view.IncludedRemoveButton)
                .DisposeWith(d);

            InitView();
            this.WhenAnyValue(x => x.ViewModel!.Status)
                .SubscribeWithErrorLogging(_ => { UpdateView(); })
                .DisposeWith(d);
        });
    }

    private void InitView()
    {
        if (ViewModel!.IsRoot)
        {
            MakeTextBlockBold();
            FileNameTextBlock.Text = Language.TreeEntryView_FileNameTextBlock_All_mod_files;
            InstallRoundedButtonTextBlock.Text = Language.TreeEntryView_InstallRoundedButtonTextBlock_Install_all;
        }
        else
        {
            FileNameTextBlock.Text = ViewModel!.FileName;

            if (ViewModel!.IsDirectory)
            {
                FolderEntryIcon.IsVisible = true;
                MakeTextBlockBold();
                InstallRoundedButtonTextBlock.Text =
                    Language.TreeEntryView_InstallRoundedButtonTextBlock_Install_folder;
            }
            else
            {
                FileEntryIcon.IsVisible = true;
                InstallRoundedButtonTextBlock.Text = Language.TreeEntryView_InstallRoundedButtonTextBlock_Install;
            }
        }

        UpdateView();
    }

    private void UpdateView()
    {
        var status = ViewModel!.Status;
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
                if (ViewModel.IsDirectory)
                {
                    IncludeTransitionButtonTextBlock.Text =
                        Language.TreeEntryView_IncludeTransitionButtonTextBlock_Include_folder;
                }
                else
                {
                    IncludeTransitionButtonTextBlock.Text = ViewModel.IsTopLevel
                        ? Language.TreeEntryView_IncludeTransitionButtonTextBlock_Include
                        : Language.TreeEntryView_IncludeTransitionButtonTextBlock_Include_with_folder;
                }

                IncludeTransitionButton.IsVisible = true;
                break;

            case ModContentNodeStatus.IncludedExplicit:
                RemoveFromLocationButtonTextBlock.Text = ViewModel.LinkedDirectoryName;
                RemoveFromLocationButton.IsVisible = true;
                break;

            case ModContentNodeStatus.IncludedViaParent:
                if (ViewModel.IsDirectory)
                {
                    IncludedRemoveButtonTextBlock.Text =
                        Language.TreeEntryView_IncludedRemoveButtonTextBlock_Included_folder;
                }
                else
                {
                    IncludedRemoveButtonTextBlock.Text = ViewModel.IsTopLevel
                        ? Language.TreeEntryView_IncludedRemoveButtonTextBlock_Included
                        : Language.TreeEntryView_IncludedRemoveButtonTextBlock_Included_with_folder;
                }

                IncludedRemoveButton.IsVisible = true;
                break;
        }
    }

    private void ClearAllButtons()
    {
        InstallRoundedButton.IsVisible = false;
        SelectLocationRoundedButton.IsVisible = false;
        RemoveFromLocationButton.IsVisible = false;
        IncludeTransitionButton.IsVisible = false;
        IncludedRemoveButton.IsVisible = false;
    }

    private void MakeTextBlockBold()
    {
        FileNameTextBlock.Classes.Remove("BodyMDNormal");
        FileNameTextBlock.Classes.Add("BodyMDBold");
    }
}
