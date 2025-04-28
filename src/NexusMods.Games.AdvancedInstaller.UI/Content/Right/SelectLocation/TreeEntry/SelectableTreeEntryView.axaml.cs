using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

[ExcludeFromCodeCoverage]
public partial class SelectableTreeEntryView : ReactiveUserControl<ISelectableTreeEntryViewModel>
{
    public SelectableTreeEntryView()
    {
        InitializeComponent();

        this.WhenActivated(disposable =>
        {
            // When the ViewModel not null, initialize the view.
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Do(InitView)
                .Subscribe()
                .DisposeWith(disposable);

            this.BindCommand(ViewModel, vm =>
                    vm.CreateMappingCommand, v => v.SelectRoundedButton)
                .DisposeWith(disposable);

            this.BindCommand(ViewModel, vm =>
                    vm.EditCreateFolderCommand, v => v.CreateFolderButton)
                .DisposeWith(disposable);

            this.BindCommand(ViewModel, vm =>
                    vm.SaveCreatedFolderCommand, v => v.SaveCreatedFolderButton)
                .DisposeWith(disposable);

            this.BindCommand(ViewModel, vm =>
                    vm.CancelCreateFolderCommand, v => v.CancelCreateFolderButton)
                .DisposeWith(disposable);

            this.BindCommand(ViewModel, vm =>
                    vm.DeleteCreatedFolderCommand, v => v.DeleteCreatedFolderButton)
                .DisposeWith(disposable);

            // Two way binding for the input text.
            this.Bind(ViewModel, vm =>
                    vm.InputText, v => v.CreateFolderNameTextBox.Text)
                .DisposeWith(disposable);


            // When state changes from Create to Editing or vice versa, update the view.
            this.WhenAnyValue(x => x.ViewModel!.Status)
                .Subscribe(UpdateView)
                .DisposeWith(disposable);
        });
    }

    private void UpdateView(SelectableDirectoryNodeStatus status)
    {
        // There are only two cases where the state can change:
        // 1. From Create to Editing
        // 2. From Editing back to Create
        // Entries with other states are created with those states and never change it.
        switch (status)
        {
            case SelectableDirectoryNodeStatus.Editing:
                // Remove Create state stuff
                CreateFolderButton.IsVisible = false;

                // Add Editing state stuff
                FileElementGrid.IsVisible = true;
                FolderEntryIcon.IsVisible = true;

                // buttons
                CancelCreateFolderButton.IsVisible = true;
                SaveCreatedFolderButton.IsVisible = true;
                SaveCreatedFolderButton.IsDefault = true;

                // input field
                CreateFolderNameTextBox.IsVisible = true;
                CreateFolderNameTextBox.Focus();
                break;

            case SelectableDirectoryNodeStatus.Create:
                // Remove Editing state stuff
                FileElementGrid.IsVisible = false;
                FolderEntryIcon.IsVisible = false;

                // input field
                CreateFolderNameTextBox.IsVisible = false;

                // buttons
                CancelCreateFolderButton.IsVisible = false;
                SaveCreatedFolderButton.IsVisible = false;
                SaveCreatedFolderButton.IsDefault = false;

                // Add Create stuff
                CreateFolderButton.IsVisible = true;
                break;
            default: break;
        }
    }

    private void InitView(ISelectableTreeEntryViewModel vm)
    {
        switch (vm.Status)
        {
            case SelectableDirectoryNodeStatus.Regular or SelectableDirectoryNodeStatus.RegularFromMapping:
                FileElementGrid.IsVisible = true;
                FolderEntryIcon.IsVisible = true;
                FileNameTextBlock.IsVisible = true;

                FileNameTextBlock.Text = vm.DisplayName;

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
                FileNameTextBlock.Text = vm.DisplayName;
                SelectRoundedButton.IsVisible = true;
                DeleteCreatedFolderButton.IsVisible = true;
                break;
        }
    }
}
