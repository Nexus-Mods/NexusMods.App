using System.Windows.Input;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface IApplyButtonViewModel : IViewModelInterface
{
    ICommand ApplyCommand { get; }
}
