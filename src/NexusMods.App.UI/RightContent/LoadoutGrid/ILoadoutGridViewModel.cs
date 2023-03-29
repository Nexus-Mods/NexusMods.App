using System.Collections.ObjectModel;
using Avalonia.Controls;
using DynamicData;
using DynamicData.Binding;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid;

public interface ILoadoutGridViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<ModCursor> Mods { get; }
    public LoadoutId Loadout { get; set; }

    public ReadOnlyObservableCollection<DataGridColumn> Columns { get; }

}
