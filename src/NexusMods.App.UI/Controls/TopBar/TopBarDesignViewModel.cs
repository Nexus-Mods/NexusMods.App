using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.TopBar;

public class TopBarDesignViewModel : AViewModel<ITopBarViewModel>, ITopBarViewModel
{
    [Reactive]
    public bool ShowWindowControls { get; set; }

    [Reactive]
    public bool IsLoggedIn { get; set; }

    [Reactive] public bool IsPremium { get; set; } = true;

    [Reactive] public IImage Avatar { get; set; } = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));

    [Reactive]
    public IAddPanelDropDownViewModel AddPanelDropDownViewModel { get; set; } = new AddPanelDropDownDesignViewModel();

    [Reactive] public ReactiveCommand<Unit, Unit> LoginCommand { get; set; } = Initializers.EnabledReactiveCommand;

    [Reactive] public ReactiveCommand<Unit, Unit> LogoutCommand { get; set; } = Initializers.EnabledReactiveCommand;

    [Reactive]
    public ReactiveCommand<Unit, Unit> MinimizeCommand { get; set; } = ReactiveCommand.Create(() => { });

    [Reactive]
    public ReactiveCommand<Unit, Unit> ToggleMaximizeCommand { get; set; } = ReactiveCommand.Create(() => { });

    [Reactive] public ReactiveCommand<Unit, Unit> CloseCommand { get; set; } = ReactiveCommand.Create(() => { });

    public TopBarDesignViewModel()
    {
        this.WhenActivated(disposables =>
        {
            LogoutCommand = ReactiveCommand.Create(ToggleLogin, this.WhenAnyValue(vm => vm.IsLoggedIn)).DisposeWith(disposables);
            LoginCommand = ReactiveCommand.Create(ToggleLogin, this.WhenAnyValue(vm => vm.IsLoggedIn).Select(x => !x)).DisposeWith(disposables);
        });
    }

    private void ToggleLogin()
    {
        IsLoggedIn = !IsLoggedIn;
    }
}
