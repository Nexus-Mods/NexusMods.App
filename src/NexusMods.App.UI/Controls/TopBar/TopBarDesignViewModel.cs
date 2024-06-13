using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.TopBar;

public class TopBarDesignViewModel : AViewModel<ITopBarViewModel>, ITopBarViewModel
{
    [Reactive] public bool IsLoggedIn { get; set; }
    [Reactive] public bool IsPremium { get; set; } = true;
    [Reactive] public IImage Avatar { get; set; } = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
    [Reactive] public string ActiveWorkspaceTitle { get; set; } = "HOME";

    [Reactive] public IAddPanelDropDownViewModel AddPanelDropDownViewModel { get; set; } = new AddPanelDropDownDesignViewModel();

    public ReactiveCommand<NavigationInformation, Unit> OpenSettingsCommand => ReactiveCommand.Create<NavigationInformation, Unit>(_ => Unit.Default);

    public ReactiveCommand<NavigationInformation, Unit> ViewChangelogCommand  => ReactiveCommand.Create<NavigationInformation, Unit>(_ => Unit.Default);
    public ReactiveCommand<Unit, Unit> ViewAppLogsCommand => Initializers.DisabledReactiveCommand;
    public ReactiveCommand<Unit, Unit> GiveFeedbackCommand => Initializers.DisabledReactiveCommand;

    public ReactiveCommand<Unit, Unit> LoginCommand { get; set; }
    public ReactiveCommand<Unit, Unit> LogoutCommand { get; set; }
    public ReactiveCommand<Unit, Unit> OpenNexusModsProfileCommand => Initializers.DisabledReactiveCommand;
    public ReactiveCommand<Unit, Unit> OpenNexusModsAccountSettingsCommand => Initializers.DisabledReactiveCommand;

    public TopBarDesignViewModel()
    {
        LogoutCommand = ReactiveCommand.Create(ToggleLogin, this.WhenAnyValue(vm => vm.IsLoggedIn));
        LoginCommand = ReactiveCommand.Create(ToggleLogin, this.WhenAnyValue(vm => vm.IsLoggedIn).Select(x => !x));
    }

    private void ToggleLogin() { IsLoggedIn = !IsLoggedIn; }
}
