using System.Reactive;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.TopBar;

public class TopBarDesignViewModel : AViewModel<ITopBarViewModel>, ITopBarViewModel
{
    [Reactive]
    public bool ShowWindowControls { get; set; }

    [Reactive]
    public bool IsLoggedIn { get; set; }

    [Reactive]
    public bool IsPremium { get; set; }

    [Reactive]
    public IImage Avatar { get; set; }

    [Reactive]
    public ReactiveCommand<Unit, Unit> LoginCommand { get; set; }

    [Reactive]
    public ReactiveCommand<Unit, Unit> LogoutCommand { get; set; }

    [Reactive]
    public ReactiveCommand<Unit, Unit> MinimizeCommand { get; set; } = ReactiveCommand.Create(() => { });

    [Reactive]
    public ReactiveCommand<Unit, Unit> ToggleMaximizeCommand { get; set; } = ReactiveCommand.Create(() => { });

    [Reactive] public ReactiveCommand<Unit, Unit> CloseCommand { get; set; } = ReactiveCommand.Create(() => { });

    public TopBarDesignViewModel()
    {
        Avatar = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
        IsLoggedIn = false;
        IsPremium = true;

        LogoutCommand = ReactiveCommand.Create(ToggleLogin, this.WhenAnyValue(vm => vm.IsLoggedIn));
        LoginCommand = ReactiveCommand.Create(ToggleLogin, this.WhenAnyValue(vm => vm.IsLoggedIn)
            .Select(x => !x));
    }

    private void ToggleLogin()
    {
        IsLoggedIn = !IsLoggedIn;
    }
}
