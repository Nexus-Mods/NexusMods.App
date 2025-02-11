using System.Globalization;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LibraryPage;

[UsedImplicitly]
public partial class LibraryView : ReactiveUserControl<ILibraryViewModel>
{
    public LibraryView()
    {
        InitializeComponent();

        TreeDataGridViewHelper.SetupTreeDataGridAdapter<LibraryView, ILibraryViewModel, CompositeItemModel<EntityId>, EntityId>(this, TreeDataGridLibrary, vm => vm.Adapter);

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

            this.OneWayBind(ViewModel, vm => vm.Collections, view => view.Collections.ItemsSource)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Collections.Count, view => view.ExpanderCollections.IsVisible, static count => count > 0)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Collections.Count, view => view.TextNumCollections.Text, static i => i.ToString("N0"))
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Adapter.SourceCount.Value, view => view.TextNumMods.Text, static i => i.ToString("N0"))
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.RemoveSelectedItemsCommand, view => view.RemoveModButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.InstallSelectedItemsCommand, view => view.AddModButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.InstallSelectedItemsWithAdvancedInstallerCommand, view => view.AddModAdvancedButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.OpenFilePickerCommand, view => view.GetModsFromDriveButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.OpenNexusModsCommand, view => view.GetModsFromNexusButton)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Adapter.Source.Value, view => view.TreeDataGridLibrary.Source)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Adapter.IsSourceEmpty.Value, view => view.EmptyState.IsActive)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.EmptyLibrarySubtitleText, view => view.EmptyLibraryTextBlock.Text)
                .AddTo(disposables);
            
            this.BindCommand(ViewModel, vm => vm.OpenNexusModsCommand, view => view.EmptyLibraryLinkButton)
                .AddTo(disposables);
            
            this.BindCommand(ViewModel, vm => vm.RefreshUpdatesCommand, view => view.Refresh)
                .AddTo(disposables);
            
            this.BindCommand(ViewModel, vm => vm.UpdateAllCommand, view => view.UpdateAll)
                .AddTo(disposables);
        });
    }
}
