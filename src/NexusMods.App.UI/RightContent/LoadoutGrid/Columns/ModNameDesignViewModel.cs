using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class ModNameDesignViewModel : AViewModel<IModNameViewModel>, IModNameViewModel
{
    public IId Row { get; set; } = new Id64(EntityCategory.TestData, 1);

    [Reactive]
    public string Name { get; set; } = "";

    public ModNameDesignViewModel()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .Select(modId => $"Name for ({modId})")
                .BindToUi(this, vm => vm.Name)
                .DisposeWith(d);
        });
    }
}
