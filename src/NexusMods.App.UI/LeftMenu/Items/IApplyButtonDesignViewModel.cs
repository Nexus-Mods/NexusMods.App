using System.Windows.Input;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public class IApplyButtonDesignViewModel : AViewModel<IApplyButtonViewModel>, IApplyButtonViewModel
{
    public ICommand ApplyCommand { get; } = ReactiveCommand.Create(() => { });
}
