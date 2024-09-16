using NexusMods.Abstractions.Loadouts;

namespace NexusMods.App.UI.Controls.DataGrid.Columns.ModName;

/// <summary>
/// Displays the name of a mod.
/// </summary>
public interface IModNameViewModel : IColumnViewModel<LoadoutItemGroupId>, ICellViewModel<string>;
