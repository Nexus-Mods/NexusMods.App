using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using System.Reactive;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.DebugControls;

public interface IDebugControlsPageViewModel : IPageViewModelInterface
{
    public ReactiveCommand<Unit, Unit> GenerateUnhandledException { get; }
    
    public IWindowManager WindowManager { get; }
    
    public IServiceProvider ServiceProvider { get; }
    
    IMarkdownRendererViewModel MarkdownRenderer { get; }
}

public class DebugControlsPageViewModel : APageViewModel<IDebugControlsPageViewModel>, IDebugControlsPageViewModel
{
    public IServiceProvider ServiceProvider { get; }
    
    public DebugControlsPageViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider) : base(windowManager)
    {
        TabTitle = "Debug Controls";
        TabIcon = IconValues.ColorLens;
        
        WindowManager = windowManager;

        GenerateUnhandledException = ReactiveCommand.Create(() => throw new Exception("Help me! This is an unhandled exception"));
        
        ServiceProvider = serviceProvider;
        
        var markdownRendererViewModel = ServiceProvider.GetRequiredService<IMarkdownRendererViewModel>();
        markdownRendererViewModel.Contents = MarkdownRendererViewModel.DebugText;
        MarkdownRenderer = markdownRendererViewModel;
    }

    public IWindowManager WindowManager { get; }

    public ReactiveCommand<Unit, Unit> GenerateUnhandledException { get; }

    public IMarkdownRendererViewModel MarkdownRenderer { get; }
}

public class DebugControlsPageDesignViewModel : APageViewModel<IDebugControlsPageViewModel>, IDebugControlsPageViewModel
{
    public DebugControlsPageDesignViewModel() : base(new DesignWindowManager()) { }
    public ReactiveCommand<Unit, Unit> GenerateUnhandledException { get; }= ReactiveCommand.Create(() => { });
    public IWindowManager WindowManager { get; } = new DesignWindowManager();
    public IServiceProvider ServiceProvider { get; } = null!;

    public IMarkdownRendererViewModel MarkdownRenderer { get; } = new MarkdownRendererViewModel() 
    {
        Contents = MarkdownRendererViewModel.DebugText,
    };
}
