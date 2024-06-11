using System.Collections.ObjectModel;
using System.Reactive;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutGrid;

/// <summary>
/// View model for the loadout grid.
/// </summary>
public interface ILoadoutGridViewModel : IPageViewModelInterface
{
    public ReadOnlyObservableCollection<ModId> Mods { get; }
    public LoadoutId LoadoutId { get; set; }
    public string LoadoutName { get; }
    
    public IMarkdownRendererViewModel MarkdownRendererViewModel { get; }

    public ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> Columns { get; }

    public ModId[] SelectedItems { get; set; }

    public ReactiveCommand<NavigationInformation, Unit> ViewModContentsCommand { get; }

    /// <summary>
    /// Delete the mods from the loadout.
    /// </summary>
    /// <param name="modsToDelete"></param>
    /// <param name="commitMessage"></param>
    /// <returns></returns>
    public Task DeleteMods(IEnumerable<ModId> modsToDelete, string commitMessage);
}
