using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class ModCategoryViewModel : AViewModel<IModCategoryViewModel>, IModCategoryViewModel
{
    [Reactive] public ModCursor Row { get; set; } = Initializers.ModCursor;

    [Reactive] public string Category { get; set; } = "";

    public ModCategoryViewModel(LoadoutRegistry loadoutRegistry, IDataStore store)
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(loadoutRegistry.Revisions)
                .Select(id => store.Get<Mod>(id))
                .WhereNotNull()
                .Select(revision => revision.ModCategory)
                .BindToUi(this, vm => vm.Category)
                .DisposeWith(d);
        });
    }
}
