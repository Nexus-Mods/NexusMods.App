using System.Globalization;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Resources;
using NexusMods.MnemonicDB.Abstractions;
using ObservableCollections;
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
                // initially hidden
                ContextControlGroup.IsVisible = false;

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

                this.OneWayBind(ViewModel, vm => vm.Collections.Count, view => view.CollectionsTabItem.IsVisible,
                        static count => count > 0
                    )
                    .AddTo(disposables);

                this.OneWayBind(ViewModel, vm => vm.Collections.Count, view => view.TextNumCollections.Text,
                        static i => i.ToString("N0")
                    )
                    .AddTo(disposables);

                this.OneWayBind(ViewModel, vm => vm.Adapter.SourceCount.Value, view => view.TextNumMods.Text,
                        static i => i.ToString("N0")
                    )
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.RemoveSelectedItemsCommand, view => view.RemoveModButton)
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.DeselectItemsCommand, view => view.DeselectItemsButton)
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.InstallSelectedItemsCommand, view => view.InstallModMenuItem)
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.InstallSelectedItemsWithAdvancedInstallerCommand, view => view.AdvancedInstallModMenuItem)
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.UpdateSelectedItemsCommand, view => view.UpdateModMenuItem)
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.UpdateAndKeepOldSelectedItemsCommand, view => view.UpdateAndKeepOldModMenuItem)
                    .AddTo(disposables);

                this.OneWayBind(ViewModel,
                        vm => vm.UpdatableSelectionCount,
                        view => view.UpdateButton.Text,
                        count => count > 0 ? count.ToString() : "")
                    .AddTo(disposables);

                this.OneWayBind(ViewModel,
                        vm => vm.UpdatableSelectionCount,
                        view => view.UpdateButton.IsVisible,
                        count => count > 0)
                    .AddTo(disposables);

                // Bind menu item headers to show real-time counts
                this.OneWayBind(ViewModel,
                        vm => vm.UpdatableSelectionCount,
                        view => view.UpdateModMenuItem.Header,
                        count => string.Format(Language.Library_Update, count))
                    .AddTo(disposables);

                this.OneWayBind(ViewModel,
                        vm => vm.UpdatableSelectionCount,
                        view => view.UpdateAndKeepOldModMenuItem.Header,
                        count => string.Format(Language.Library_UpdateAndKeepOldMod, count))
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.OpenFilePickerCommand, view => view.GetModsFromDriveButton)
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.OpenNexusModsCommand, view => view.GetModsFromNexusButton)
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.OpenNexusModsCollectionsCommand, view => view.GetCollectionFromNexusButton)
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

                this.WhenAnyValue(view => view.ViewModel!.InstallationTargets.Count)
                    .OnUI()
                    .SubscribeWithErrorLogging(count =>
                    {
                        InstallationTargetControlGroup.IsVisible = count > 1;
                    }).AddTo(disposables);

                this.OneWayBind(ViewModel, vm => vm.InstallationTargets, view => view.InstallationTargetSelector.ItemsSource)
                    .AddTo(disposables);

                this.Bind(ViewModel, vm => vm.SelectedInstallationTarget, view => view.InstallationTargetSelector.SelectedItem)
                    .AddTo(disposables);

                InstallationTargetSelector.SelectedIndex = 0;

                this.WhenAnyValue(view => view.ViewModel!.SelectionCount)
                    .WhereNotNull()
                    .SubscribeWithErrorLogging(count =>
                        {
                            ContextControlGroup.IsVisible = count != 0;

                            if (count != 0)
                            {
                                DeselectItemsButton.Text = string.Format(Language.Library_DeselectItemsButton_Text, count);
                            }
                        }
                    )
                    .AddTo(disposables);
            }
        );
    }
}
