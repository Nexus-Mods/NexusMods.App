using System.Reactive;
using Avalonia.Media;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.TopBar;

public interface ITopBarViewModel : IViewModelInterface
{
    public string ActiveWorkspaceTitle { get; }

    public string ActiveWorkspaceSubtitle { get; }

    public ReactiveCommand<NavigationInformation, Unit> OpenSettingsCommand { get; }

    public ReactiveCommand<NavigationInformation, Unit> ViewChangelogCommand { get; }
    public ReactiveCommand<Unit, Unit> ViewAppLogsCommand { get; }
    public ReactiveCommand<Unit, Unit> GiveFeedbackCommand { get; }

    public ReactiveCommand<Unit, Unit> LoginCommand { get; }
    public ReactiveCommand<Unit, Unit> LogoutCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenNexusModsProfileCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenNexusModsPremiumCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenNexusModsAccountSettingsCommand { get; }

    public bool IsLoggedIn { get; }
    public UserRole UserRole { get; }
    public IImage? Avatar { get; }
    public string? Username { get; }

    public IAddPanelDropDownViewModel AddPanelDropDownViewModel { get; set; }

    public IPanelTabViewModel? SelectedTab { get; set; }
}
