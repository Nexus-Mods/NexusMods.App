﻿using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModVersion;

/// <summary>
/// Displays the version of a mod.
/// </summary>
public interface IModVersionViewModel : IColumnViewModel<ModId>, ICellViewModel<string>;
