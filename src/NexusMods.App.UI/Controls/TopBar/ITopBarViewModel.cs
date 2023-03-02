using System.Reactive;
using System.Windows.Input;
using Avalonia.Media;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.TopBar;

public interface ITopBarViewModel : IViewModelInterface
{
    public bool IsLoggedIn { get; }
    public bool IsPremium { get; }
    public IImage Avatar { get; }
    
    public ReactiveCommand<Unit, Unit> LoginCommand { get; }
    public ReactiveCommand<Unit, Unit> LogoutCommand { get; }
    
    public ReactiveCommand<Unit, Unit> MinimizeCommand { get; }
    public ReactiveCommand<Unit, Unit> MaximizeCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
}