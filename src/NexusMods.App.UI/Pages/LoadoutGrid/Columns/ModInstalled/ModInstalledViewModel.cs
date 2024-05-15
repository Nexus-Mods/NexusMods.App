using System.Reactive.Disposables;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModInstalled;

[UsedImplicitly]
internal class ModInstalledViewModel(IConnection connection) : 
    AColumnViewModel<IModInstalledViewModel, DateTime>(connection), IModInstalledViewModel
{
    protected override DateTime Selector(Mod.Model model) => model.GetCreatedAt();

    protected override int Compare(DateTime a, DateTime b) => DateTime.Compare(a, b);
}
