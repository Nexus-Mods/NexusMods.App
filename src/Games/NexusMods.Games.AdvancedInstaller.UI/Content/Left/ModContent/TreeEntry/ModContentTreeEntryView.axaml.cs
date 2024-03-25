using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls.Trees.Common;
using NexusMods.Games.AdvancedInstaller.UI.Resources;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

[ExcludeFromCodeCoverage]
public partial class
    ModContentTreeEntryView : ReactiveUserControl<IModContentTreeEntryViewModel>
{
    public ModContentTreeEntryView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            // Initialize the view if the view model is not null
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Do(InitView)
                .Subscribe()
                .DisposeWith(d);

            // Update the view when the status changes
            this.WhenAnyValue(view => view.ViewModel!.Status)
                .Subscribe(_ => { UpdateView(ViewModel!); })
                .DisposeWith(d);

            // Command bindings:
            this.BindCommand(ViewModel, vm => vm.BeginSelectCommand,
                    view => view.InstallRoundedButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.CancelSelectCommand,
                    view => view.SelectLocationRoundedButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.CancelSelectCommand,
                    view => view.IncludeTransitionButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.RemoveMappingCommand,
                    view => view.RemoveFromLocationButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.RemoveMappingCommand,
                    view => view.IncludedRemoveButton)
                .DisposeWith(d);
        });
    }

    private void InitView(IModContentTreeEntryViewModel vm)
    {
        if (vm.IsRoot)
        {
            EntryIcon.IsVisible = false;
            MakeTextBlockBold();
            FileNameTextBlock.Text = Language.TreeEntryView_FileNameTextBlock_All_mod_files;
            InstallRoundedButtonTextBlock.Text = Language.TreeEntryView_InstallRoundedButtonTextBlock_Install_all;
        }
        else
        {
            FileNameTextBlock.Text = vm.FileName;

            if (vm.IsDirectory)
            {
                EntryIcon.Classes.Add("FolderOutline");
                MakeTextBlockBold();
                InstallRoundedButtonTextBlock.Text =
                    Language.TreeEntryView_InstallRoundedButtonTextBlock_Install_folder;
            }
            else
            {
                EntryIcon.Classes.Add(vm.FileName.GetIconClassFromFileName());
                InstallRoundedButtonTextBlock.Text = Language.TreeEntryView_InstallRoundedButtonTextBlock_Install;
            }
        }

        UpdateView(vm);
    }

    private void UpdateView(IModContentTreeEntryViewModel vm)
    {
        var status = vm.Status;
        ClearAllButtons();
        switch (status)
        {
            case ModContentTreeEntryStatus.Default:
                InstallRoundedButton.IsVisible = true;
                break;

            case ModContentTreeEntryStatus.Selecting:
                SelectLocationRoundedButton.IsVisible = true;
                break;

            case ModContentTreeEntryStatus.SelectingViaParent:
                if (vm.IsDirectory)
                {
                    IncludeTransitionButtonTextBlock.Text =
                        Language.TreeEntryView_IncludeTransitionButtonTextBlock_Include_folder;
                }
                else
                {
                    IncludeTransitionButtonTextBlock.Text = vm.IsTopLevelChild
                        ? Language.TreeEntryView_IncludeTransitionButtonTextBlock_Include
                        : Language.TreeEntryView_IncludeTransitionButtonTextBlock_Include_with_folder;
                }

                IncludeTransitionButton.IsVisible = true;
                break;

            case ModContentTreeEntryStatus.IncludedExplicit:
                RemoveFromLocationButtonTextBlock.Text = vm.MappingFolderName;
                RemoveFromLocationButton.IsVisible = true;
                break;

            case ModContentTreeEntryStatus.IncludedViaParent:
                if (vm.IsDirectory)
                {
                    IncludedRemoveButtonTextBlock.Text =
                        Language.TreeEntryView_IncludedRemoveButtonTextBlock_Included_folder;
                }
                else
                {
                    IncludedRemoveButtonTextBlock.Text = vm.IsTopLevelChild
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
