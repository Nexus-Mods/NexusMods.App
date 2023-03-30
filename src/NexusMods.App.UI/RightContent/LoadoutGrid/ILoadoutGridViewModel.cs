using System.Collections.ObjectModel;
using Avalonia.Controls;
using DynamicData;
using DynamicData.Binding;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid;

public interface ILoadoutGridViewModel : IRightContentViewModel
{
    public ReadOnlyObservableCollection<ModCursor> Mods { get; }
    public LoadoutId Loadout { get; set; }

    public ReadOnlyObservableCollection<IDataGridColumnFactory> Columns { get; }

}
