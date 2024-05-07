using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModVersion;

public class ModVersionViewModel(IConnection conn) : AColumnViewModel<IModVersionViewModel, string>(conn), IModVersionViewModel
{
    protected override string Selector(Mod.Model model) => model.Version;

    protected override int Compare(string a, string b) =>
        string.Compare(a, b, StringComparison.Ordinal);
}
