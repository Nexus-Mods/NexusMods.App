using System.Windows.Input;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface IApplyControlViewModel : IViewModelInterface
{
    ICommand ApplyCommand { get; }
}
