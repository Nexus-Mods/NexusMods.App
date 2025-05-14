using System.ComponentModel;
using System.Reactive;
using Avalonia.Threading;
using ExCSS;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public class MessageBoxViewModel : IDialogViewModel<ButtonDefinitionId>
{
    public MessageBoxButtonDefinition[] ButtonDefinitions { get; }
    public ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId> CloseWindowCommand { get; }
    
    public string WindowTitle { get; }
    public double WindowWidth { get; }
    public string Text { get; set; }
    public string? Heading { get; set; }
    
    /// <summary>
    /// If provided, this will be displayed in a markdown control below the description. Use this
    /// for more descriptive information.
    /// </summary>
    public IMarkdownRendererViewModel? MarkdownRenderer { get; set; }
    public IconValue? Icon { get; }
    public MessageBoxSize MessageBoxSize { get; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public ViewModelActivator Activator { get; } = null!;

    public IDialogView<ButtonDefinitionId>? View { get; set; }
    public ButtonDefinitionId Result { get; set; }
    public IDialogContentViewModel? ContentViewModel { get; set; }

    public MessageBoxViewModel(
        string title,
        string text,
        MessageBoxButtonDefinition[] buttonDefinitions,
        string? heading = null,
        IconValue? icon = null,
        MessageBoxSize messageBoxSize = MessageBoxSize.Small,
        IDialogContentViewModel? contentViewModel = null,
        IMarkdownRendererViewModel? markdownRendererViewModel = null)
    {
        WindowTitle = title;
        Text = text;
        Heading = heading;
        ButtonDefinitions = buttonDefinitions;
        MessageBoxSize = messageBoxSize;
        WindowWidth = messageBoxSize switch
        {
            MessageBoxSize.Small => 320,
            MessageBoxSize.Medium => 480,
            MessageBoxSize.Large => 640,
            _ => 320,
        };

        Icon = icon;
        ContentViewModel = contentViewModel;
        MarkdownRenderer = markdownRendererViewModel;

        CloseWindowCommand = ReactiveCommand.Create<ButtonDefinitionId, ButtonDefinitionId>((id) =>
            {
                Result = id;
                return id;
            }
        );
    }
}
