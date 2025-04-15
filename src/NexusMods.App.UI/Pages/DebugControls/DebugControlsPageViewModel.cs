using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using System.Reactive;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.MessageBox.Enums;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.DebugControls;

public interface IDebugControlsPageViewModel : IPageViewModelInterface
{
    public ReactiveCommand<Unit, Unit> ShowModalOK { get; }
    public ReactiveCommand<Unit, Unit> ShowModalOKCancel { get; }
    public ReactiveCommand<Unit, Unit> ShowModeless { get; }
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

        GenerateUnhandledException = ReactiveCommand.Create(() => throw new Exception("Help me! This is an unhandled exception"));
        
        var markdownRendererViewModel = serviceProvider.GetRequiredService<IMarkdownRendererViewModel>();
        markdownRendererViewModel.Contents = MarkdownRendererViewModel.DebugText;
        MarkdownRenderer = markdownRendererViewModel;
        
        ShowModalOK = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await windowManager.ShowModalAsync("Test Modal", "This is a modal", ButtonEnum.Ok);
                
                // Handle the result of the dialog
                Console.WriteLine(result);
            }
        );
        
        ShowModalOKCancel = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await windowManager.ShowModalAsync("Test Modal", "This is a modal", ButtonEnum.OkCancel);
                
                // Handle the result of the dialog
                Console.WriteLine(result);
            }
        );
        
        ShowModeless = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await windowManager.ShowModelessAsync("Test Modeless", "This is a modeless", ButtonEnum.OkCancel);
                
                // Handle the result of the dialog
                Console.WriteLine(result);
            }
        );
    }

    public ReactiveCommand<Unit, Unit> ShowModalOK { get; }
    public ReactiveCommand<Unit, Unit> ShowModalOKCancel { get; }
    public ReactiveCommand<Unit, Unit> ShowModeless { get; }
    public ReactiveCommand<Unit, Unit> GenerateUnhandledException { get; }

    public IMarkdownRendererViewModel MarkdownRenderer { get; }
}

public class DebugControlsPageDesignViewModel : APageViewModel<IDebugControlsPageViewModel>, IDebugControlsPageViewModel
{
    public DebugControlsPageDesignViewModel() : base(new DesignWindowManager()) { }
    
    public ReactiveCommand<Unit, Unit> ShowModalOK { get; } = ReactiveCommand.CreateFromTask(() => Task.CompletedTask);
    public ReactiveCommand<Unit, Unit> ShowModalOKCancel { get; } = ReactiveCommand.CreateFromTask(() => Task.CompletedTask);
    public ReactiveCommand<Unit, Unit> ShowModeless { get; } = ReactiveCommand.CreateFromTask(() => Task.CompletedTask);
    public ReactiveCommand<Unit, Unit> GenerateUnhandledException { get; }= ReactiveCommand.Create(() => { });

    public IMarkdownRendererViewModel MarkdownRenderer { get; } = new MarkdownRendererViewModel() 
    {
        Contents = MarkdownRendererViewModel.DebugText,
    };
}
