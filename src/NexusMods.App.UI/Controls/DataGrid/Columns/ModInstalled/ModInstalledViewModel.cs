using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.App.UI.Controls.DataGrid.Columns;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModInstalled;

[UsedImplicitly]
internal class ModInstalledViewModel(IConnection connection) : 
    AColumnViewModel<IModInstalledViewModel, DateTime>(connection), IModInstalledViewModel
{
    protected override DateTime Selector(LoadoutItemGroup.ReadOnly model) => model.GetCreatedAt();

    protected override int Compare(DateTime a, DateTime b) => DateTime.Compare(a, b);
}
