using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.WorkspaceSystem;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Observable = R3.Observable;
using ReactiveCommand = ReactiveUI.ReactiveCommand;
using Unit = System.Reactive.Unit;

namespace NexusMods.App.UI.Controls.TopBar;

public class TopBarDesignViewModel : AViewModel<ITopBarViewModel>, ITopBarViewModel
{
    [Reactive] public bool IsLoggedIn { get; set; } = true;
    [Reactive] public UserRole UserRole { get; set; } = UserRole.Free;
    [Reactive] public string? Username { get; set; } = "insomnious";
    [Reactive] public IImage Avatar { get; set; } = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
    [Reactive] public string ActiveWorkspaceTitle { get; set; } = "Home";
    [Reactive] public string ActiveWorkspaceSubtitle { get; set; } = "Loadout A";

    [Reactive] public IAddPanelDropDownViewModel AddPanelDropDownViewModel { get; set; } = new AddPanelDropDownDesignViewModel();

    public ReactiveUI.ReactiveCommand<NavigationInformation, Unit> OpenSettingsCommand => ReactiveCommand.Create<NavigationInformation, Unit>(_ => Unit.Default);

    public ReactiveUI.ReactiveCommand<NavigationInformation, Unit> ViewChangelogCommand => ReactiveCommand.Create<NavigationInformation, Unit>(_ => Unit.Default);
    public ReactiveUI.ReactiveCommand<Unit, Unit> ViewAppLogsCommand => Initializers.DisabledReactiveCommand;
    public ReactiveUI.ReactiveCommand<Unit, Unit> ShowWelcomeMessageCommand => Initializers.EnabledReactiveCommand;
    public ReactiveUI.ReactiveCommand<Unit, Unit> OpenDiscordCommand => ReactiveCommand.Create(() => {});
    public ReactiveUI.ReactiveCommand<Unit, Unit> OpenForumsCommand => ReactiveCommand.Create(() => {});
    public ReactiveUI.ReactiveCommand<Unit, Unit> OpenGitHubCommand => ReactiveCommand.Create(() => {});
    public ReactiveUI.ReactiveCommand<Unit, Unit> OpenStatusPageCommand => ReactiveCommand.Create(() => {});

    public R3.ReactiveCommand<R3.Unit, R3.Unit> LoginCommand { get; set; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> LogoutCommand { get; set; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> OpenNexusModsProfileCommand => Initializers.DisabledReactiveCommand;
    public ReactiveUI.ReactiveCommand<Unit, Unit> OpenNexusModsPremiumCommand => Initializers.EnabledReactiveCommand;
    public ReactiveUI.ReactiveCommand<Unit, Unit> OpenNexusModsAccountSettingsCommand => Initializers.DisabledReactiveCommand;

    public IPanelTabViewModel? SelectedTab { get; set; }

    public TopBarDesignViewModel()
    {
        LogoutCommand = ReactiveCommand.Create(ToggleLogin, this.WhenAnyValue(vm => vm.IsLoggedIn));
        LoginCommand = ReactiveCommandExtensions.ToReactiveCommand<R3.Unit, R3.Unit>(Observable.ToObservable(this.WhenAnyValue(vm => vm.IsLoggedIn).Select(x => !x)), convert:
            _ =>
            {
                ToggleLogin();
                return R3.Unit.Default;
            });
    }

    private void ToggleLogin()
    {
        IsLoggedIn = !IsLoggedIn;
    }
}
