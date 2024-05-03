using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModName;

public class ModNameDesignViewModel : AColumnViewModel<IModNameViewModel, string>
{
    public ModNameDesignViewModel() : base()
    {
    }

    protected override string Selector(Mod.Model model)
    {
        throw new NotImplementedException();
    }

    protected override int Compare(string a, string b)
    {
        throw new NotImplementedException();
    }
}
