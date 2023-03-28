using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class ModeNameDesignViewModel : AViewModel<IModNameViewModel>, IModNameViewModel
{
    public ModId Row { get; set; } =
        ModId.From(new Guid("00000000-0000-0000-0000-000000000001"));

    [Reactive]
    public string Name { get; set; } = "";

    public ModeNameDesignViewModel()
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
