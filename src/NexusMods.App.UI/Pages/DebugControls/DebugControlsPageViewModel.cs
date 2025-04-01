using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.AlphaWarning;
using NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using System.Reactive;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.DebugControls;

public interface IDebugControlsPageViewModel : IPageViewModelInterface
{
    public ReactiveCommand<Unit, Unit> ShowAlphaViewCommand { get; }   
    public ReactiveCommand<Unit, Unit> ShowMessageBoxOKCommand { get; }      
}

public class DebugControlsPageViewModel : APageViewModel<IDebugControlsPageViewModel>, IDebugControlsPageViewModel
{
    public DebugControlsPageViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider) : base(windowManager)
    {
        TabTitle = "Debug Controls";
        TabIcon = IconValues.ColorLens;
        
        //var workspaceController = windowManager.ActiveWorkspaceController;
        var overlayController = serviceProvider.GetRequiredService<IOverlayController>();
        
        ShowAlphaViewCommand = ReactiveCommand.Create(() =>
        {
            var alphaWarningViewModel = serviceProvider.GetRequiredService<IAlphaWarningViewModel>();
            //alphaWarningViewModel.WorkspaceController = workspaceController;

            overlayController.Enqueue(alphaWarningViewModel);
        });
        
        ShowMessageBoxOKCommand = ReactiveCommand.Create(() =>
        {
            //var messageBoxOkViewModel = serviceProvider.GetRequiredService<IMessageBoxOkViewModel>();
            //alphaWarningViewModel.WorkspaceController = workspaceController;

            //overlayController.Enqueue(messageBoxOkViewModel);
            
            Task.Run(() => MessageBoxOkViewModel.Show(serviceProvider, "Simon", "Great", "### h3 hello"));
        });
    }

    public ReactiveCommand<Unit, Unit> ShowAlphaViewCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowMessageBoxOKCommand { get; }
}
