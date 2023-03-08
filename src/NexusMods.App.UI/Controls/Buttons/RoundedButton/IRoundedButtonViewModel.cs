using System.Windows.Input;

namespace NexusMods.App.UI.Controls.Buttons.RoundedButton;

public interface IRoundedButtonViewModel : IViewModelInterface
{
    public string Name { get; }
    public ICommand Command { get; }
}
