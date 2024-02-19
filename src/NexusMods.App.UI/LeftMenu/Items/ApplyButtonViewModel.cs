using System.Windows.Input;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public class ApplyButtonViewModel : AViewModel<IApplyButtonViewModel>, IApplyButtonViewModel
{
    public ICommand ApplyCommand { get; }

    public ApplyButtonViewModel()
    {
        ApplyCommand = ReactiveCommand.Create(() => { });
    }
}
