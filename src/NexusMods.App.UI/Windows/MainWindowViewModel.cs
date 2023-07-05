using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.Login;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Windows;

public class MainWindowViewModel : AViewModel<IMainWindowViewModel>
{
    private readonly INexusLoginOverlayViewModel _nexusOverlayViewModel;
    private readonly IOverlayController _overlayController;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        IOSInformation osInformation,
        ISpineViewModel spineViewModel,
        ITopBarViewModel topBarViewModel,
        INexusLoginOverlayViewModel nexusOverlayViewModel,
        IOverlayController controller)
    {
        TopBar = topBarViewModel;
        Spine = spineViewModel;
        _overlayController = controller;
        _nexusOverlayViewModel = nexusOverlayViewModel;

        // Only show controls in Windows since we can remove the chrome on that platform
        TopBar.ShowWindowControls = osInformation.IsWindows;

        this.WhenActivated(d =>
        {
            Spine.Actions
                .SubscribeWithErrorLogging(logger, HandleSpineAction)
                .DisposeWith(d);

            _overlayController.ApplyNextOverlay.Subscribe(item =>
                {
                    if (item == null)
                    {
                        OverlayContent = null;
                        return;
                    }

                    // This is the main window, if no reference control is specified, show it here.
                    if (item.Value.ViewItem == null)
                        OverlayContent = item.Value.VM;
                    else
                    {
                        // TODO: Determine if we are the right window. For now we do nothing, until that helper is implemented
                        OverlayContent = item.Value.VM;
                    }
                })
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.Spine.LeftMenu)
                .Select(left =>
                {
                    logger.LogDebug("Spine changed left menu to {LeftMenu}", left);
                    return left;
                })
                .BindTo(this, vm => vm.LeftMenu)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.LeftMenu.RightContent)
                .Select(right =>
                {
                    logger.LogDebug(
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

    [Reactive]
    public IOverlayViewModel? OverlayContent { get; set; }
}
