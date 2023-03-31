﻿using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

/// <summary>
/// Displays the category of a mod.
/// </summary>
public interface IModCategoryViewModel : IColumnViewModel<ModCursor>
{
    public string Category { get; }
}
