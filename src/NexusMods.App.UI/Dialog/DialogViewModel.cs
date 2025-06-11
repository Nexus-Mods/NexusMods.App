using System.ComponentModel;
using System.Reactive;
using Avalonia.Threading;
using ExCSS;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public class DialogViewModel : IDialogViewModel<ButtonDefinitionId>
{
    public DialogButtonDefinition[] ButtonDefinitions { get; }
    public ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId> CloseWindowCommand { get; }
    
    public string WindowTitle { get; }
    public double WindowWidth { get; }
    public string? Text { get; set; }
    public string? Heading { get; set; }
    
    /// <summary>
    /// If provided, this will be displayed in a markdown control below the description. Use this
    /// for more descriptive information.
    /// </summary>
    public IMarkdownRendererViewModel? MarkdownRenderer { get; set; }
    public IconValue? Icon { get; }
    public DialogWindowSize DialogWindowSize { get; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public ViewModelActivator Activator { get; } = null!;

    public IDialogView<ButtonDefinitionId>? View { get; set; }
    public ButtonDefinitionId Result { get; set; }
    public IViewModelInterface? ContentViewModel { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogViewModel"/> class.
    /// </summary>
    /// <param name="title">The title of the message box window.</param>
    /// <param name="text">The main text content of the message box.</param>
    /// <param name="buttonDefinitions">An array of button definitions for the message box.</param>
    /// <param name="heading">An optional heading for the message box.</param>
    /// <param name="icon">An optional icon to display in the message box.</param>
    /// <param name="dialogWindowSize">The size of the message box (Small, Medium, or Large).</param>
    /// <param name="markdownRendererViewModel">An optional markdown renderer for additional descriptive information.</param>
    /// <param name="contentViewModel">An optional content view model for custom content. If this is set, only title and buttonDefinitions are additionally used.</param>
    public DialogViewModel(
        string title,
        DialogButtonDefinition[] buttonDefinitions,
        string? text = null,
        string? heading = null,
        IconValue? icon = null,
        DialogWindowSize dialogWindowSize = DialogWindowSize.Small,
        IMarkdownRendererViewModel? markdownRendererViewModel = null,
        IViewModelInterface? contentViewModel = null)
    {
        WindowTitle = title;
        Text = text;
        Heading = heading;
        ButtonDefinitions = buttonDefinitions;
        DialogWindowSize = dialogWindowSize;
        WindowWidth = dialogWindowSize switch
        {
            DialogWindowSize.Small => 320,
            DialogWindowSize.Medium => 480,
            DialogWindowSize.Large => 640,
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
