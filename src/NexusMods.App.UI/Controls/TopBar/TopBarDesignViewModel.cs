using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

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

    public ReactiveCommand<NavigationInformation, Unit> OpenSettingsCommand => ReactiveCommand.Create<NavigationInformation, Unit>(_ => Unit.Default);

    public ReactiveCommand<NavigationInformation, Unit> ViewChangelogCommand => ReactiveCommand.Create<NavigationInformation, Unit>(_ => Unit.Default);
    public ReactiveCommand<Unit, Unit> ViewAppLogsCommand => Initializers.DisabledReactiveCommand;
    public ReactiveCommand<Unit, Unit> ShowWelcomeMessageCommand => Initializers.EnabledReactiveCommand;
    public ReactiveCommand<Unit, Unit> OpenDiscordCommand => ReactiveCommand.Create(() => {});
    public ReactiveCommand<Unit, Unit> OpenForumsCommand => ReactiveCommand.Create(() => {});
    public ReactiveCommand<Unit, Unit> OpenGitHubCommand => ReactiveCommand.Create(() => {});
    public ReactiveCommand<Unit, Unit> OpenStatusPageCommand => ReactiveCommand.Create(() => {});

    public ReactiveCommand<Unit, Unit> LoginCommand { get; set; }
    public ReactiveCommand<Unit, Unit> LogoutCommand { get; set; }
    public ReactiveCommand<Unit, Unit> OpenNexusModsProfileCommand => Initializers.DisabledReactiveCommand;
    public ReactiveCommand<Unit, Unit> OpenNexusModsPremiumCommand => Initializers.EnabledReactiveCommand;
    public ReactiveCommand<Unit, Unit> OpenNexusModsAccountSettingsCommand => Initializers.DisabledReactiveCommand;

    public IPanelTabViewModel? SelectedTab { get; set; }

    public TopBarDesignViewModel()
    {
        LogoutCommand = ReactiveCommand.Create(ToggleLogin, this.WhenAnyValue(vm => vm.IsLoggedIn));
        LoginCommand = ReactiveCommand.Create(ToggleLogin, this.WhenAnyValue(vm => vm.IsLoggedIn).Select(x => !x));
    }

    private void ToggleLogin()
    {
        IsLoggedIn = !IsLoggedIn;
    }
}
