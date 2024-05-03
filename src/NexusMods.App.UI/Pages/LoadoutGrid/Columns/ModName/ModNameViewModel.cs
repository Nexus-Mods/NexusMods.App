using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModName;

public class ModNameViewModel(IConnection conn) : AColumnViewModel<IModNameViewModel, string>(conn)
{
    protected override string Selector(Mod.Model model) => model.Name;

    protected override int Compare(string a, string b) =>
        string.Compare(a, b, StringComparison.Ordinal);
}
