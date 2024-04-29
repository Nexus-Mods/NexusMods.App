using System.Reactive;
using Avalonia.Media;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.TopBar;

public interface ITopBarViewModel : IViewModelInterface
{
    public bool ShowWindowControls { get; set; }
    public bool IsLoggedIn { get; }
    public bool IsPremium { get; }
    public IImage Avatar { get; }
    public string ActiveWorkspaceTitle { get; }

    public IAddPanelDropDownViewModel AddPanelDropDownViewModel { get; set; }

    public ReactiveCommand<Unit, Unit> LoginCommand { get; }
    public ReactiveCommand<Unit, Unit> LogoutCommand { get; }

    public ReactiveCommand<Unit, Unit> MinimizeCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleMaximizeCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public ReactiveCommand<Unit, Unit> HistoryActionCommand { get; }
    public ReactiveCommand<Unit, Unit> UndoActionCommand { get; }
    public ReactiveCommand<Unit, Unit> RedoActionCommand { get; }
    public ReactiveCommand<Unit, Unit> HelpActionCommand { get; }
    public ReactiveCommand<NavigationInformation, Unit> SettingsActionCommand { get; }


}
