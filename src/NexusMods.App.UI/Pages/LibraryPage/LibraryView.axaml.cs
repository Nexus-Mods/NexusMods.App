using Avalonia.Controls;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LibraryPage;

[UsedImplicitly]
public partial class LibraryView : ReactiveUserControl<ILibraryViewModel>
{
    public LibraryView()
    {
        InitializeComponent();

        TreeDataGridViewHelper.SetupTreeDataGridAdapter<LibraryView, ILibraryViewModel, LibraryItemModel>(this, TreeDataGrid, vm => vm.Adapter);

        this.WhenActivated(disposables =>
        {
            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider is not null)
            {
                this.WhenAnyValue(view => view.ViewModel)
                    .WhereNotNull()
                    .SubscribeWithErrorLogging(vm => vm.StorageProvider = storageProvider)
                    .AddTo(disposables);
            }

            this.BindCommand(ViewModel, vm => vm.SwitchViewCommand, view => view.SwitchView)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.InstallSelectedItemsCommand, view => view.AddModButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.InstallSelectedItemsWithAdvancedInstallerCommand, view => view.AddModAdvancedButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.OpenFilePickerCommand, view => view.GetModsFromDriveButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.OpenNexusModsCommand, view => view.GetModsFromNexusButton)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Adapter.Source.Value, view => view.TreeDataGrid.Source)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Adapter.IsSourceEmpty.Value, view => view.EmptyState.IsActive)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.EmptyLibrarySubtitleText, view => view.EmptyLibrarySubtitleTextBlock.Text)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.OpenNexusModsCommand, view => view.EmptyLibraryLinkButton)
                .AddTo(disposables);
        });
    }
}
