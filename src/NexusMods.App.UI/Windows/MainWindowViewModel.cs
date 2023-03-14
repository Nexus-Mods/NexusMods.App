using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Windows;

public class MainWindowViewModel : AViewModel<IMainWindowViewModel>
{
    private readonly ILogger<MainWindowViewModel> _logger;

    public MainWindowViewModel(ILogger<MainWindowViewModel> logger, ISpineViewModel spineViewModel, ITopBarViewModel topBarViewModel)
    {
        _logger = logger;
        TopBar = topBarViewModel;
        Spine = spineViewModel;

        // Only show controls in Windows since we can remove the chrome on that platform
        TopBar.ShowWindowControls = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        this.WhenActivated(d =>
        {
            Spine.Actions
                .Subscribe(HandleSpineAction)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.Spine.LeftMenu)
                .Select(left =>
                {
                    _logger.LogDebug("Spine changed left menu to {LeftMenu}", left);
                    return left;
                })
                .BindTo(this, vm => vm.LeftMenu)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.LeftMenu.RightContent)
                .Select(right =>
                {
                    _logger.LogDebug(
                        "Left menu changed right content to {RightContent}",
                        right);
                    return right;
                }).BindTo(this, vm => vm.RightContent);
        });
    }

    private void HandleSpineAction(SpineButtonAction action)
    {
        Spine.Activations.OnNext(action);
    }

    [Reactive]
    public ISpineViewModel Spine { get; set; }

    [Reactive]
    public IViewModelInterface RightContent { get; set; } =
        Initializers.IRightContent;

    [Reactive]
    public ILeftMenuViewModel LeftMenu { get; set; } =
        Initializers.ILeftMenuViewModel;

    [Reactive]
    public ITopBarViewModel TopBar { get; set; }
}
