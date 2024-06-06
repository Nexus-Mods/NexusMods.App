using System.Reactive;
using Avalonia.Media;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.TopBar;

public interface ITopBarViewModel : IViewModelInterface
{
    public string ActiveWorkspaceTitle { get; }

    public ReactiveCommand<Unit, Unit> LoginCommand { get; }
    public ReactiveCommand<Unit, Unit> LogoutCommand { get; }

    public bool IsLoggedIn { get; }
    public bool IsPremium { get; }
    public IImage? Avatar { get; }

    public IAddPanelDropDownViewModel AddPanelDropDownViewModel { get; set; }
    public ReactiveCommand<Unit, Unit> HelpActionCommand { get; }
    public ReactiveCommand<NavigationInformation, Unit> OpenSettingsCommand { get; }
}
