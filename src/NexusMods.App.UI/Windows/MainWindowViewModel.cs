using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.Overlays;
using NexusMods.Common;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Windows;

public class MainWindowViewModel : AViewModel<IMainWindowViewModel>
{
    private readonly INexusLoginOverlayViewModel _nexusOverlayViewModel;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        IOSInformation osInformation,
        ISpineViewModel spineViewModel,
        ITopBarViewModel topBarViewModel,
        INexusLoginOverlayViewModel nexusOverlayViewModel)
    {
        TopBar = topBarViewModel;
        Spine = spineViewModel;
        _nexusOverlayViewModel = nexusOverlayViewModel;

        // Only show controls in Windows since we can remove the chrome on that platform
        TopBar.ShowWindowControls = osInformation.IsWindows;

        this.WhenActivated(d =>
        {
            Spine.Actions
                .SubscribeWithErrorLogging(logger, HandleSpineAction)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm._nexusOverlayViewModel.IsActive)
                .Select(active => active ? _nexusOverlayViewModel : null)
                .BindTo(this, vm => vm.OverlayContent)
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
    
    // TODO: We should probably have some sort of common 'stack' of modals/overlays.
    public void SetOverlayContent(IOverlayViewModel vm)
    {
        // Register overlay close.
        var unsubscribeToken = new CancellationTokenSource();
        vm.WhenAnyValue(x => x.IsActive)
            .OnUI()
            .Subscribe(b =>
            {
                if (b) 
                    return;
                
                // On unsubscribe (IsActive == false), kill the overlay.
                OverlayContent = null;
                unsubscribeToken.Cancel();
            }, unsubscribeToken.Token);
        
        // Set the new overlay.
        OverlayContent = vm;
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
