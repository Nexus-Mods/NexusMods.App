using System.Windows.Input;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public class IApplyControlDesignViewModel : AViewModel<IApplyControlViewModel>, IApplyControlViewModel
{
    public ICommand ApplyCommand { get; } = ReactiveCommand.Create(() => { });
}
