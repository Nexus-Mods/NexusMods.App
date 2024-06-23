using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Resources;
using ReactiveUI;
using static NexusMods.App.UI.Controls.DataGrid.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid;

[UsedImplicitly]
public partial class LoadoutGridView : ReactiveUserControl<ILoadoutGridViewModel>
{
    public LoadoutGridView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Mods.Count)
                .Select(count => count == 0)
                .BindToView(this, view => view.EmptyState.IsActive)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.EmptyModlistTitleMessage, view => view.EmptyState.Header)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Mods)
                .BindToView(this, view => view.ModsDataGrid.ItemsSource)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.ViewModContentsCommand, view => view.ViewModFilesButton)
                .DisposeWith(d);
            
            this.BindCommand(ViewModel, vm => vm.ViewModLibraryCommand, view => view.ViewModLibraryButton)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Columns)
                .GenerateColumns(ModsDataGrid)
                .DisposeWith(d);

            Observable.FromEventPattern<SelectionChangedEventArgs>(
                addHandler => ModsDataGrid.SelectionChanged += addHandler,
                removeHandler => ModsDataGrid.SelectionChanged -= removeHandler)
                .Select(_ => ModsDataGrid.SelectedItems.Cast<ModId>().ToArray())
                .BindTo(ViewModel, vm => vm.SelectedItems)
                .DisposeWith(d);

            // TODO: remove these commands and move all of this into the ViewModel
            var isItemSelected = this.WhenAnyValue(
                view => view.ModsDataGrid.SelectedIndex,
                (selectedIndex) => selectedIndex >= 0);

            DeleteModsButton.Command = ReactiveCommand.CreateFromTask(DeleteSelectedMods, isItemSelected);
        });
    }

    private async Task DeleteSelectedMods()
    {
        var toDelete = new List<ModId>();
        foreach (var row in ModsDataGrid.SelectedItems)
        {
            if (row is not ModId id) continue;
            toDelete.Add(id);
        }
        await ViewModel!.DeleteMods(toDelete, "Deleted by user via UI.");
    }
}

