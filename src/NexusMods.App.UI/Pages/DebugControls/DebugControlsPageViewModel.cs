using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;
using System.Reactive;
using Avalonia.Controls.Notifications;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.DebugControls;

public interface IDebugControlsPageViewModel : IPageViewModelInterface
{
    public ReactiveCommand<Unit, Unit> GenerateUnhandledException { get; }
    
    public IWindowManager WindowManager { get; }
    
    public IServiceProvider ServiceProvider { get; }
    
    IMarkdownRendererViewModel MarkdownRenderer { get; }

    public ReactiveCommand<Unit, Unit> ShowInfoNotificationCommand { get; }
}

public class DebugControlsPageViewModel : APageViewModel<IDebugControlsPageViewModel>, IDebugControlsPageViewModel
{
    public IServiceProvider ServiceProvider { get; }
    
    public DebugControlsPageViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        IWindowNotificationService windowNotificationService) : base(windowManager)
    {
        var windowNotificationService1 = windowNotificationService;
        
        TabTitle = "Debug Controls";
        TabIcon = IconValues.ColorLens;
        
        WindowManager = windowManager;

        GenerateUnhandledException = ReactiveCommand.Create(() => throw new Exception("Help me! This is an unhandled exception"));
        ShowInfoNotificationCommand = ReactiveCommand.Create(() =>
        {
            windowNotificationService1?.Show(
                "This is an info toast notification",
                ToastNotificationVariant.Neutral
            );
        });
        
        ServiceProvider = serviceProvider;
        
        var markdownRendererViewModel = ServiceProvider.GetRequiredService<IMarkdownRendererViewModel>();
        markdownRendererViewModel.Contents = MarkdownRendererViewModel.DebugText;
        MarkdownRenderer = markdownRendererViewModel;
    }

    public IWindowManager WindowManager { get; }

    public ReactiveCommand<Unit, Unit> GenerateUnhandledException { get; }

    public IMarkdownRendererViewModel MarkdownRenderer { get; }
    public ReactiveCommand<Unit, Unit> ShowInfoNotificationCommand { get; }
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

    public ReactiveCommand<Unit, Unit> ShowInfoNotificationCommand { get; } = ReactiveCommand.Create(() => { });
}
