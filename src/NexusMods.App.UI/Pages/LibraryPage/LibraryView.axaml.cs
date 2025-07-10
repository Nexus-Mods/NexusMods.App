using System.Globalization;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Filters;
using NexusMods.App.UI.Controls.TreeDataGrid.Filters;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Resources;
using NexusMods.MnemonicDB.Abstractions;
using ObservableCollections;
using R3;
using ReactiveUI;
using static NexusMods.App.UI.Controls.Filters.Filter;

namespace NexusMods.App.UI.Pages.LibraryPage;

[UsedImplicitly]
public partial class LibraryView : ReactiveUserControl<ILibraryViewModel>
{

    static LibraryView()
    {
        // Allo focus on the LibraryView for keyboard shortcuts purposes
        FocusableProperty.OverrideDefaultValue(typeof(LibraryView), true); 
    }

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

                this.WhenAnyValue(view => view.SearchTextBox.Text)
                    .OnUI()
                    .Subscribe(searchText =>
                    {
                        if (ViewModel?.Adapter != null)
                        {
                            ViewModel.Adapter.Filter.Value = string.IsNullOrWhiteSpace(searchText)
                                ? NoFilter.Instance
                                : new Filter.TextFilter(searchText, CaseSensitive: false);
                        }
                    })
                    .AddTo(disposables);

                // Clear button functionality
                SearchClearButton.Click += (_, _) =>
                {
                    ClearSearch();
                };

                // Handle focus when search panel visibility changes
                this.WhenAnyValue(view => view.SearchPanel.IsVisible)
                    .Skip(1) // Skip the initial value to avoid focusing on startup
                    .Subscribe(isVisible =>
                    {
                        if (isVisible)
                        {
                            // Focus the textbox when the search panel becomes visible
                            SearchTextBox.Focus();

                            // Tracking
                            ViewModel?.Adapter?.OnOpenSearchPanel("Library");
                        }
                        else
                        {
                            // When unfocused, with CTRL+F, user should be able to scroll with keyboard.
                            // But I don't know how to restore the focus yet (and it would take longer to find out)
                        }
                    })
                    .AddTo(disposables);

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

    private void ClearSearch()
    {
        SearchTextBox.Text = string.Empty;
        // Also collapse the search panel
        SearchPanel.IsVisible = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Handle Ctrl+F to toggle search panel
        if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            ToggleSearchPanelVisibility();
            e.Handled = true; // Prevent the event from bubbling up
            return;
        }

        if (e.Key == Key.Escape && SearchPanel.IsVisible)
        {
            ClearSearch();
            e.Handled = true; // Prevent the event from bubbling up
            return;
        }

        base.OnKeyDown(e);
    }

    private void SearchButton_OnClick(object? sender, RoutedEventArgs e) => ToggleSearchPanelVisibility();
    private void ToggleSearchPanelVisibility() => SearchPanel.IsVisible = !SearchPanel.IsVisible;
}
