using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.AlphaWarning;
using NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using System.Reactive;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.DebugControls;

public interface IDebugControlsPageViewModel : IPageViewModelInterface
{
    public ReactiveCommand<Unit, Unit> GenerateUnhandledException { get; }
    
    IMarkdownRendererViewModel MarkdownRenderer { get; }
}

public class DebugControlsPageViewModel : APageViewModel<IDebugControlsPageViewModel>, IDebugControlsPageViewModel
{
    public DebugControlsPageViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider) : base(windowManager)
    {
        TabTitle = "Debug Controls";
        TabIcon = IconValues.ColorLens;

        var overlayController = serviceProvider.GetRequiredService<IOverlayController>();

        GenerateUnhandledException = ReactiveCommand.Create(() => throw new Exception("Help me! This is an unhandled exception"));
        
        var markdownRendererViewModel = serviceProvider.GetRequiredService<IMarkdownRendererViewModel>();
        markdownRendererViewModel.Contents = MarkdownRendererViewModel.DebugText;
        MarkdownRenderer = markdownRendererViewModel;
    }

    public ReactiveCommand<Unit, Unit> GenerateUnhandledException { get; }

    public IMarkdownRendererViewModel MarkdownRenderer { get; }
}

public class DebugControlsPageDesignViewModel : APageViewModel<IDebugControlsPageViewModel>, IDebugControlsPageViewModel
{
    public DebugControlsPageDesignViewModel() : base(new DesignWindowManager()) { }
    
    public ReactiveCommand<Unit, Unit> GenerateUnhandledException { get; }= ReactiveCommand.Create(() => { });

    public IMarkdownRendererViewModel MarkdownRenderer { get; } = new MarkdownRendererViewModel() 
    {
        Contents = MarkdownRendererViewModel.DebugText,
    };
}
