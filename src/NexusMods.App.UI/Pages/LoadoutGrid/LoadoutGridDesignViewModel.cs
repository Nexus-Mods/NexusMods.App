using System.Collections.ObjectModel;
using System.Reactive;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutGrid;

/// <summary>
///     This is a dummy VM to allow for basic designer functionality.
/// </summary>
[UsedImplicitly]
public class LoadoutGridDesignViewModel(IWindowManager windowManager) : APageViewModel<ILoadoutGridViewModel>(windowManager), ILoadoutGridViewModel
{
    public ReadOnlyObservableCollection<ModId> Mods { get; } = new([]);
    public LoadoutId LoadoutId { get; set; }
    public string LoadoutName { get; } = "Design Loadout";
    public IMarkdownRendererViewModel MarkdownRendererViewModel { get; } = new MarkdownRendererDesignViewModel();
    public ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> Columns { get; } = new([]);
    public ModId[] SelectedItems { get; set; } = [];
    public ReactiveCommand<NavigationInformation, Unit> ViewModContentsCommand { get; } = ReactiveCommand.Create<NavigationInformation, Unit>(_ => Unit.Default);

    public LoadoutGridDesignViewModel() : this(new DesignWindowManager()) { }

    public Task AddMod(string path) => throw new NotImplementedException();
    public Task AddModAdvanced(string path) => throw new NotImplementedException();
    public Task DeleteMods(IEnumerable<ModId> modsToDelete, string commitMessage) => throw new NotImplementedException();
}
