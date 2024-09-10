using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Controls.DataGrid.Columns.ModName;

[UsedImplicitly]
internal class ModNameViewModel(IConnection conn) : AColumnViewModel<IModNameViewModel, string>(conn), IModNameViewModel
{
    protected override string Selector(LoadoutItemGroup.ReadOnly model) => model.AsLoadoutItem().Name;

    protected override int Compare(string a, string b) => string.Compare(a, b, StringComparison.Ordinal);
}
